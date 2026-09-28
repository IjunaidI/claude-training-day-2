using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AssetDesk.Data;

public partial class AssetRepository(string connectionString)
{
    private const string AssetColumns =
        """
        id, tag, category, make, model, serial, status, condition,
        purchase_date, cost, location, notes, assigned_to, assigned_date
        """;

    private SqliteConnection Connect()
    {
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }

    public AppState GetState()
    {
        using var conn = Connect();

        var employeeRows = conn.Query<EmployeeRow>(
            "SELECT id, name, email, department, title FROM employees ORDER BY name");

        var assetRows = conn.Query<AssetRow>($"SELECT {AssetColumns} FROM assets ORDER BY tag");

        return new AppState(
            employeeRows.Select(MapEmployee).ToList(),
            assetRows.Select(MapAsset).ToList());
    }

    public List<Asset> CreateAssets(NewAssetInput input)
    {
        // ValidateNewAsset(input);

        var tags = ExpandTags(input.Tag, input.Quantity);
        var serials = ExpandSerials(input.Serial, input.Quantity);

        using var conn = Connect();
        using var tx = conn.BeginTransaction();

        var ids = new List<string>();
        for (var i = 0; i < input.Quantity; i++)
        {
            var id = Guid.NewGuid().ToString();
            try
            {
                conn.Execute(
                    $"""
                    INSERT INTO assets ({AssetColumns})
                    VALUES (@Id, @Tag, @Category, @Make, @Model, @Serial, 'in_stock', @Condition,
                            @PurchaseDate, @Cost, @Location, @Notes, NULL, NULL)
                    """,
                    new
                    {
                        Id = id,
                        Tag = tags[i],
                        Category = Format.ToDb(input.Category),
                        input.Make,
                        input.Model,
                        Serial = serials[i],
                        Condition = Format.ToDb(input.Condition),
                        input.PurchaseDate,
                        input.Cost,
                        input.Location,
                        Notes = input.Notes,
                    },
                    tx);
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067) // UNIQUE
            {
                throw new AssetDeskException($"Tag {tags[i]} is already in use.");
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 275) // CHECK
            {
                throw new AssetDeskException("Cost must be a number of 0 or more.");
            }
            ids.Add(id);
        }

        tx.Commit();

        return conn.Query<AssetRow>(
                $"SELECT {AssetColumns} FROM assets WHERE id IN @Ids ORDER BY tag",
                new { Ids = ids })
            .Select(MapAsset)
            .ToList();
    }

    public Asset Assign(string assetId, string employeeId, string assignedDate)
    {
        using var conn = Connect();
        using var tx = conn.BeginTransaction();

        var current = conn.QuerySingleOrDefault<TagStatusRow>(
            "SELECT tag, status FROM assets WHERE id = @assetId", new { assetId }, tx);
        if (current is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        if (current.status != "in_stock")
        {
            var label = Format.Label(Format.StatusFromDb(current.status)).ToLowerInvariant();
            throw new AssetDeskException($"{current.tag} is {label} and cannot be assigned.");
        }

        try
        {
            conn.Execute(
                """
                UPDATE assets
                SET status = 'assigned', assigned_to = @employeeId, assigned_date = @assignedDate
                WHERE id = @assetId
                """,
                new { employeeId, assignedDate, assetId },
                tx);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 787) // FOREIGN KEY
        {
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");
        }

        tx.Commit();
        return GetAssetOrThrow(conn, assetId);
    }

    public Asset Return(string assetId)
    {
        using var conn = Connect();
        using var tx = conn.BeginTransaction();

        var exists = conn.ExecuteScalar<string?>(
            "SELECT status FROM assets WHERE id = @assetId", new { assetId }, tx);
        if (exists is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        conn.Execute(
            "UPDATE assets SET status = 'in_stock', assigned_to = NULL, assigned_date = NULL WHERE id = @assetId",
            new { assetId },
            tx);

        tx.Commit();
        return GetAssetOrThrow(conn, assetId);
    }

    public Asset SetStatus(string assetId, Status status)
    {
        using var conn = Connect();
        using var tx = conn.BeginTransaction();

        var currentDb = conn.ExecuteScalar<string?>(
            "SELECT status FROM assets WHERE id = @assetId", new { assetId }, tx);
        if (currentDb is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        var newDb = Format.ToDb(status);
        if (newDb == "retired" && currentDb == "assigned")
        {
            conn.Execute(
                "UPDATE assets SET status = @newDb, assigned_to = NULL, assigned_date = NULL WHERE id = @assetId",
                new { newDb, assetId },
                tx);
        }
        else
        {
            conn.Execute(
                "UPDATE assets SET status = @newDb WHERE id = @assetId",
                new { newDb, assetId },
                tx);
        }

        tx.Commit();
        return GetAssetOrThrow(conn, assetId);
    }

    private static void ValidateNewAsset(NewAssetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Tag)) throw new AssetDeskException("Tag is required.");
        if (string.IsNullOrWhiteSpace(input.Make)) throw new AssetDeskException("Make is required.");
        if (string.IsNullOrWhiteSpace(input.Model)) throw new AssetDeskException("Model is required.");
        if (string.IsNullOrWhiteSpace(input.Serial)) throw new AssetDeskException("Serial is required.");
        if (string.IsNullOrWhiteSpace(input.PurchaseDate)) throw new AssetDeskException("Purchase date is required.");
        if (string.IsNullOrWhiteSpace(input.Location)) throw new AssetDeskException("Location is required.");
        if (input.Quantity is < 1 or > 20) throw new AssetDeskException("Quantity must be between 1 and 20.");
    }

    private static List<string> ExpandTags(string tag, int quantity)
    {
        if (quantity == 1) return [tag];

        var match = TagSuffixRegex().Match(tag);
        if (!match.Success) return Enumerable.Repeat(tag, quantity).ToList();

        var prefix = tag[..match.Index];
        var digits = match.Value;
        var number = long.Parse(digits);

        return Enumerable.Range(0, quantity)
            .Select(i => prefix + (number + i).ToString().PadLeft(digits.Length, '0'))
            .ToList();
    }

    private static List<string> ExpandSerials(string serial, int quantity) =>
        quantity == 1
            ? [serial]
            : Enumerable.Range(1, quantity).Select(i => $"{serial}-{i}").ToList();

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TagSuffixRegex();

    private Asset GetAssetOrThrow(SqliteConnection conn, string assetId)
    {
        var row = conn.QuerySingleOrDefault<AssetRow>(
            $"SELECT {AssetColumns} FROM assets WHERE id = @assetId", new { assetId });
        if (row is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");
        return MapAsset(row);
    }

    private static Employee MapEmployee(EmployeeRow row) =>
        new(row.id, row.name, row.email, row.department, row.title);

    private static Asset MapAsset(AssetRow row) =>
        new(
            row.id,
            row.tag,
            Format.CategoryFromDb(row.category),
            row.make,
            row.model,
            row.serial,
            Format.StatusFromDb(row.status),
            Format.ConditionFromDb(row.condition),
            row.purchase_date,
            row.cost,
            row.location,
            row.notes,
            row.assigned_to,
            row.assigned_date);

    private record TagStatusRow(string tag, string status);

    private record EmployeeRow(string id, string name, string email, string department, string title);

    private record AssetRow(
        string id,
        string tag,
        string category,
        string make,
        string model,
        string serial,
        string status,
        string condition,
        string purchase_date,
        double cost,
        string location,
        string notes,
        string? assigned_to,
        string? assigned_date);
}
