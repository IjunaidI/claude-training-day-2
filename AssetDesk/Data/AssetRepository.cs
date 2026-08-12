using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AssetDesk.Data;

public class AssetRepository(string connectionString)
{
    private const string AssetColumns =
        "id, tag, category, make, model, serial, status, condition, purchase_date, cost, location, notes, assigned_to, assigned_date";

    private const string EmployeeColumns = "id, name, email, department, title";

    private readonly string _connectionString = connectionString;

    private record AssetRow(
        string id, string tag, string category, string make, string model, string serial,
        string status, string condition, string purchase_date, double cost, string location,
        string notes, string? assigned_to, string? assigned_date);

    private record EmployeeRow(string id, string name, string email, string department, string title);

    private static Asset MapAsset(AssetRow row) => new(
        row.id, row.tag, Format.CategoryFromDbValue(row.category), row.make, row.model, row.serial,
        Format.StatusFromDbValue(row.status), Format.ConditionFromDbValue(row.condition),
        row.purchase_date, row.cost, row.location, row.notes, row.assigned_to, row.assigned_date);

    private static Employee MapEmployee(EmployeeRow row) =>
        new(row.id, row.name, row.email, row.department, row.title);

    public AppState GetState()
    {
        using var conn = new SqliteConnection(_connectionString);
        var employees = conn.Query<EmployeeRow>($"SELECT {EmployeeColumns} FROM employees ORDER BY name")
            .Select(MapEmployee).ToList();
        var assets = conn.Query<AssetRow>($"SELECT {AssetColumns} FROM assets ORDER BY tag")
            .Select(MapAsset).ToList();
        return new AppState(employees, assets);
    }

    public List<Asset> CreateAssets(NewAssetInput input)
    {
        ValidateNewAssetInput(input);

        var (prefix, digitWidth, startNumber) = SplitTagSuffix(input.Tag);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var createdIds = new List<string>();

        for (var i = 0; i < input.Quantity; i++)
        {
            var tag = i == 0
                ? input.Tag
                : startNumber is not null
                    ? prefix + (startNumber.Value + i).ToString().PadLeft(digitWidth, '0')
                    : $"{input.Tag}-{i + 1}";
            var serial = input.Quantity > 1 ? $"{input.Serial}-{i + 1}" : input.Serial;
            var id = Guid.NewGuid().ToString();

            try
            {
                conn.Execute($"""
                    INSERT INTO assets ({AssetColumns})
                    VALUES (@Id, @Tag, @Category, @Make, @Model, @Serial, 'in_stock', @Condition, @PurchaseDate, @Cost, @Location, @Notes, NULL, NULL)
                    """,
                    new
                    {
                        Id = id,
                        Tag = tag,
                        Category = Format.ToDb(input.Category),
                        input.Make,
                        input.Model,
                        Serial = serial,
                        Condition = Format.ToDb(input.Condition),
                        input.PurchaseDate,
                        input.Cost,
                        input.Location,
                        input.Notes,
                    }, tx);
            }
            catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
            {
                throw new AssetDeskException($"Tag {tag} is already in use.");
            }

            createdIds.Add(id);
        }

        tx.Commit();

        return createdIds.Select(id => GetAssetById(conn, id)).ToList();
    }

    public Asset Assign(string assetId, string employeeId, string assignedDate)
    {
        if (!Format.IsValidDate(assignedDate))
            throw new AssetDeskException("Assigned date must be a valid date (YYYY-MM-DD).");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var assetRow = conn.QueryFirstOrDefault<AssetRow>(
            $"SELECT {AssetColumns} FROM assets WHERE id = @Id", new { Id = assetId }, tx);
        if (assetRow is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        var employeeExists = conn.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM employees WHERE id = @Id", new { Id = employeeId }, tx) > 0;
        if (!employeeExists)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        var asset = MapAsset(assetRow);
        if (asset.Status != Status.InStock)
            throw new AssetDeskException(
                $"{asset.Tag} is {Format.Label(asset.Status).ToLowerInvariant()} and cannot be assigned.");

        conn.Execute("""
            UPDATE assets SET status = 'assigned', assigned_to = @EmployeeId, assigned_date = @AssignedDate
            WHERE id = @Id
            """,
            new { EmployeeId = employeeId, AssignedDate = assignedDate, Id = assetId }, tx);

        tx.Commit();
        return GetAssetById(conn, assetId);
    }

    public Asset Return(string assetId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var assetRow = conn.QueryFirstOrDefault<AssetRow>(
            $"SELECT {AssetColumns} FROM assets WHERE id = @Id", new { Id = assetId }, tx);
        if (assetRow is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        conn.Execute("""
            UPDATE assets SET status = 'in_stock', assigned_to = NULL, assigned_date = NULL
            WHERE id = @Id
            """, new { Id = assetId }, tx);

        tx.Commit();
        return GetAssetById(conn, assetId);
    }

    public Asset SetStatus(string assetId, Status status)
    {
        if (status == Status.Assigned)
            throw new AssetDeskException("Status must be in stock, repair, or retired.");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        var assetRow = conn.QueryFirstOrDefault<AssetRow>(
            $"SELECT {AssetColumns} FROM assets WHERE id = @Id", new { Id = assetId }, tx);
        if (assetRow is null)
            throw new AssetDeskException("That asset no longer exists. Refresh to see current data.");

        conn.Execute("""
            UPDATE assets SET status = @Status, assigned_to = NULL, assigned_date = NULL
            WHERE id = @Id
            """, new { Status = Format.ToDb(status), Id = assetId }, tx);

        tx.Commit();
        return GetAssetById(conn, assetId);
    }

    private static Asset GetAssetById(SqliteConnection conn, string id) =>
        MapAsset(conn.QuerySingle<AssetRow>($"SELECT {AssetColumns} FROM assets WHERE id = @Id", new { Id = id }));

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
        if (!Format.IsValidDate(input.PurchaseDate))
            throw new AssetDeskException("Purchase date must be a valid date (YYYY-MM-DD).");
        if (input.Cost < 0)
            throw new AssetDeskException("Cost must be a number of 0 or more.");
        if (string.IsNullOrWhiteSpace(input.Location))
            throw new AssetDeskException("Location is required.");
        if (input.Quantity is < 1 or > 20)
            throw new AssetDeskException("Quantity must be between 1 and 20.");
    }

    private static (string Prefix, int DigitWidth, int? Number) SplitTagSuffix(string tag)
    {
        var match = Regex.Match(tag, @"^(.*?)(\d+)$");
        if (!match.Success) return (tag, 0, null);
        var digits = match.Groups[2].Value;
        return (match.Groups[1].Value, digits.Length, int.Parse(digits));
    }
}
