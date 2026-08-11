using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AssetDesk.Data;

public class AssetRepository(string connectionString)
{
    // Empirically verified against this app's Microsoft.Data.Sqlite (10.0.11): CHECK is 275, not
    // the 1811 the spec names — 1811 is SQLITE_CONSTRAINT_TRIGGER on this build. Using the value
    // that actually fires; see the CHECK/UNIQUE/FOREIGNKEY probe run before writing this file.
    private const int CheckViolation = 275;
    private const int UniqueViolation = 2067;
    private const int ForeignKeyViolation = 787;

    private const string SelectAssetColumns =
        "SELECT id AS Id, tag AS Tag, category AS Category, make AS Make, model AS Model, " +
        "serial AS Serial, status AS Status, condition AS Condition, purchase_date AS PurchaseDate, " +
        "cost AS Cost, location AS Location, notes AS Notes, assigned_to AS AssignedTo, " +
        "assigned_date AS AssignedDate FROM assets";

    private record AssetRow(
        string Id, string Tag, string Category, string Make, string Model, string Serial,
        string Status, string Condition, string PurchaseDate, double Cost, string Location,
        string Notes, string? AssignedTo, string? AssignedDate);

    private record EmployeeRow(string Id, string Name, string Email, string Department, string Title);

    private static Asset MapAsset(AssetRow r) => new(
        r.Id, r.Tag, Format.CategoryFromDb(r.Category), r.Make, r.Model, r.Serial,
        Format.StatusFromDb(r.Status), Format.ConditionFromDb(r.Condition), r.PurchaseDate,
        r.Cost, r.Location, r.Notes, r.AssignedTo, r.AssignedDate);

    private static Employee MapEmployee(EmployeeRow r) => new(r.Id, r.Name, r.Email, r.Department, r.Title);

    public AppState GetState()
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var employeeRows = conn.Query<EmployeeRow>(
            "SELECT id AS Id, name AS Name, email AS Email, department AS Department, title AS Title " +
            "FROM employees ORDER BY name");

        var assetRows = conn.Query<AssetRow>($"{SelectAssetColumns} ORDER BY tag");

        return new AppState(
            employeeRows.Select(MapEmployee).ToList(),
            assetRows.Select(MapAsset).ToList());
    }

    public List<Asset> CreateAssets(NewAssetInput input)
    {
        ValidateNewAssetInput(input);

        var (prefix, number, width) = ParseTag(input.Tag);

        var newAssets = Enumerable.Range(0, input.Quantity)
            .Select(i => (
                Id: Guid.NewGuid().ToString(),
                Tag: $"{prefix}{(number + i).ToString().PadLeft(width, '0')}",
                Serial: input.Quantity > 1 ? $"{input.Serial}-{i + 1}" : input.Serial))
            .ToList();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        const string insertSql =
            "INSERT INTO assets (id, tag, category, make, model, serial, status, condition, " +
            "purchase_date, cost, location, notes, assigned_to, assigned_date) " +
            "VALUES (@Id, @Tag, @Category, @Make, @Model, @Serial, @Status, @Condition, " +
            "@PurchaseDate, @Cost, @Location, @Notes, NULL, NULL)";

        var i = 0;
        try
        {
            for (; i < newAssets.Count; i++)
            {
                conn.Execute(insertSql, new
                {
                    newAssets[i].Id,
                    newAssets[i].Tag,
                    Category = Format.ToDb(input.Category),
                    input.Make,
                    input.Model,
                    newAssets[i].Serial,
                    Status = Format.ToDb(Status.InStock),
                    Condition = Format.ToDb(input.Condition),
                    input.PurchaseDate,
                    input.Cost,
                    input.Location,
                    input.Notes,
                }, tx);
            }
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == UniqueViolation)
        {
            tx.Rollback();
            throw new AssetDeskException($"Tag {newAssets[i].Tag} is already in use.");
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == CheckViolation)
        {
            tx.Rollback();
            throw new AssetDeskException("Cost must be a number of 0 or more.");
        }

        tx.Commit();

        return newAssets.Select(a => new Asset(
            a.Id, a.Tag, input.Category, input.Make, input.Model, a.Serial,
            Status.InStock, input.Condition, input.PurchaseDate, input.Cost,
            input.Location, input.Notes, null, null)).ToList();
    }

    public Asset Assign(string assetId, string employeeId, string assignedDate)
    {
        if (!Format.TryParseIsoDate(assignedDate, out _))
            throw new AssetDeskException("Assigned date must be a valid date.");

        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var row = conn.QuerySingleOrDefault<AssetRow>(
            $"{SelectAssetColumns} WHERE id = @Id", new { Id = assetId }, tx);
        if (row is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        var status = Format.StatusFromDb(row.Status);
        if (status != Status.InStock)
            throw new AssetDeskException(
                $"{row.Tag} is {Format.Label(status).ToLowerInvariant()} and cannot be assigned.");

        try
        {
            conn.Execute(
                "UPDATE assets SET status = @Status, assigned_to = @AssignedTo, assigned_date = @AssignedDate " +
                "WHERE id = @Id",
                new { Status = Format.ToDb(Status.Assigned), AssignedTo = employeeId, AssignedDate = assignedDate, Id = assetId },
                tx);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == ForeignKeyViolation)
        {
            tx.Rollback();
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");
        }

        tx.Commit();

        return MapAsset(row) with { Status = Status.Assigned, AssignedTo = employeeId, AssignedDate = assignedDate };
    }

    public Asset Return(string assetId)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var row = conn.QuerySingleOrDefault<AssetRow>(
            $"{SelectAssetColumns} WHERE id = @Id", new { Id = assetId }, tx);
        if (row is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        conn.Execute(
            "UPDATE assets SET status = @Status, assigned_to = NULL, assigned_date = NULL WHERE id = @Id",
            new { Status = Format.ToDb(Status.InStock), Id = assetId }, tx);

        tx.Commit();

        return MapAsset(row) with { Status = Status.InStock, AssignedTo = null, AssignedDate = null };
    }

    public Asset SetStatus(string assetId, Status status)
    {
        if (status == Status.Assigned)
            throw new AssetDeskException("Use the assign endpoint to set an asset to assigned.");

        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var row = conn.QuerySingleOrDefault<AssetRow>(
            $"{SelectAssetColumns} WHERE id = @Id", new { Id = assetId }, tx);
        if (row is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        conn.Execute(
            "UPDATE assets SET status = @Status, assigned_to = NULL, assigned_date = NULL WHERE id = @Id",
            new { Status = Format.ToDb(status), Id = assetId }, tx);

        tx.Commit();

        return MapAsset(row) with { Status = status, AssignedTo = null, AssignedDate = null };
    }

    private static void ValidateNewAssetInput(NewAssetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Tag))
            throw new AssetDeskException("Tag is required.");
        if (string.IsNullOrWhiteSpace(input.Make))
            throw new AssetDeskException("Make is required.");
        if (string.IsNullOrWhiteSpace(input.Model))
            throw new AssetDeskException("Model is required.");
        if (string.IsNullOrWhiteSpace(input.Serial))
            throw new AssetDeskException("Serial is required.");
        if (string.IsNullOrWhiteSpace(input.Location))
            throw new AssetDeskException("Location is required.");
        if (!Format.TryParseIsoDate(input.PurchaseDate, out _))
            throw new AssetDeskException("Purchase date must be a valid date.");
        if (input.Cost < 0)
            throw new AssetDeskException("Cost must be a number of 0 or more.");
        if (input.Quantity is < 1 or > 20)
            throw new AssetDeskException("Quantity must be between 1 and 20.");
    }

    private static readonly Regex TagPattern = new(@"^(.*?)(\d+)$", RegexOptions.Compiled);

    private static (string Prefix, int Number, int Width) ParseTag(string tag)
    {
        var match = TagPattern.Match(tag);
        if (!match.Success)
            throw new AssetDeskException("Tag must end with a number, e.g. AST-1001.");

        var digits = match.Groups[2].Value;
        return (match.Groups[1].Value, int.Parse(digits), digits.Length);
    }
}
