using Dapper;
using Microsoft.Data.Sqlite;

namespace AssetDesk.Data;

public static class Db
{
    public static string BuildConnectionString(string dbPath) =>
        $"Data Source={dbPath};Foreign Keys=True";

    private const string SchemaSql = """
        PRAGMA journal_mode = WAL;

        CREATE TABLE IF NOT EXISTS employees (
          id         TEXT PRIMARY KEY,
          name       TEXT NOT NULL,
          email      TEXT NOT NULL UNIQUE,
          department TEXT NOT NULL,
          title      TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS assets (
          id            TEXT PRIMARY KEY,
          tag           TEXT NOT NULL UNIQUE,
          category      TEXT NOT NULL CHECK (category IN
                          ('Laptop','Monitor','Headset','Dock','Phone','Keyboard','Other')),
          make          TEXT NOT NULL,
          model         TEXT NOT NULL,
          serial        TEXT NOT NULL,
          status        TEXT NOT NULL CHECK (status IN
                          ('in_stock','assigned','repair','retired')),
          condition     TEXT NOT NULL CHECK (condition IN ('new','good','fair','poor')),
          purchase_date TEXT NOT NULL,
          cost          REAL NOT NULL CHECK (cost >= 0),
          location      TEXT NOT NULL,
          notes         TEXT NOT NULL DEFAULT '',
          assigned_to   TEXT REFERENCES employees(id),
          assigned_date TEXT,
          CHECK (
            (status =  'assigned' AND assigned_to IS NOT NULL AND assigned_date IS NOT NULL) OR
            (status <> 'assigned' AND assigned_to IS     NULL AND assigned_date IS     NULL)
          )
        );

        CREATE INDEX IF NOT EXISTS idx_assets_status   ON assets(status);
        CREATE INDEX IF NOT EXISTS idx_assets_assigned ON assets(assigned_to);
        """;

    public static void Initialize(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        conn.Execute(SchemaSql);
        SeedIfEmpty(conn);
    }

    private static void SeedIfEmpty(SqliteConnection conn)
    {
        var assetCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM assets");
        if (assetCount > 0) return;

        var employeeIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid().ToString()).ToArray();

        using var tx = conn.BeginTransaction();

        var employees = new (string Name, string Email, string Department, string Title)[]
        {
            ("Aisha Rahman", "aisha.rahman@northwind.co", "Engineering", "Backend Engineer"),
            ("Daniel Okafor", "daniel.okafor@northwind.co", "Design", "Product Designer"),
            ("Mei Tanaka", "mei.tanaka@northwind.co", "Sales", "Account Executive"),
            ("Lucas Moreau", "lucas.moreau@northwind.co", "Engineering", "QA Engineer"),
            ("Priya Nair", "priya.nair@northwind.co", "People Ops", "HR Generalist"),
        };

        for (var i = 0; i < employees.Length; i++)
        {
            conn.Execute(
                """
                INSERT INTO employees (id, name, email, department, title)
                VALUES (@Id, @Name, @Email, @Department, @Title)
                """,
                new
                {
                    Id = employeeIds[i],
                    employees[i].Name,
                    employees[i].Email,
                    employees[i].Department,
                    employees[i].Title,
                },
                tx);
        }

        var assets = new (string Tag, string Category, string Make, string Model, string Serial,
            string Status, int? AssignedToIndex, string? AssignedDate, string Condition,
            string PurchaseDate, double Cost, string Location)[]
        {
            ("AST-1001", "Laptop", "Apple", "MacBook Pro 14 M3", "C02XK1QF", "assigned", 0, "2024-04-02", "good", "2024-03-28", 2400, "HQ / Floor 3"),
            ("AST-1002", "Laptop", "Apple", "MacBook Air 13 M2", "C02YT8LM", "assigned", 1, "2024-06-17", "good", "2024-06-10", 1350, "HQ / Floor 2"),
            ("AST-1003", "Laptop", "Lenovo", "ThinkPad X1 Carbon G11", "PF3K92XA", "in_stock", null, null, "new", "2025-01-15", 1800, "HQ / Store Room"),
            ("AST-1004", "Laptop", "Lenovo", "ThinkPad T14 G4", "PF2M40BC", "repair", null, null, "fair", "2023-09-04", 1200, "HQ / IT Bench"),
            ("AST-1005", "Monitor", "Dell", "UltraSharp U2723QE", "CN0TX41A", "assigned", 0, "2024-04-02", "good", "2024-03-28", 620, "HQ / Floor 3"),
            ("AST-1006", "Monitor", "Dell", "UltraSharp U2723QE", "CN0TX41B", "in_stock", null, null, "good", "2024-03-28", 620, "HQ / Store Room"),
            ("AST-1007", "Headset", "Sony", "WH-1000XM5", "S5H88201", "assigned", 2, "2025-02-03", "good", "2025-01-29", 380, "HQ / Floor 1"),
            ("AST-1008", "Headset", "Jabra", "Evolve2 65", "JB2065X1", "in_stock", null, null, "new", "2025-05-20", 210, "HQ / Store Room"),
            ("AST-1009", "Headset", "Jabra", "Evolve2 65", "JB2065X2", "in_stock", null, null, "new", "2025-05-20", 210, "HQ / Store Room"),
            ("AST-1010", "Dock", "CalDigit", "TS4 Thunderbolt", "CD4TS091", "assigned", 1, "2024-06-17", "good", "2024-06-10", 380, "HQ / Floor 2"),
            ("AST-1011", "Phone", "Apple", "iPhone 15", "F17GK220", "assigned", 2, "2024-11-11", "good", "2024-11-05", 900, "HQ / Floor 1"),
            ("AST-1012", "Keyboard", "Keychron", "K3 Pro", "KC3P7741", "retired", null, null, "poor", "2022-08-19", 95, "HQ / Store Room"),
        };

        foreach (var asset in assets)
        {
            conn.Execute(
                """
                INSERT INTO assets
                    (id, tag, category, make, model, serial, status, condition,
                     purchase_date, cost, location, notes, assigned_to, assigned_date)
                VALUES
                    (@Id, @Tag, @Category, @Make, @Model, @Serial, @Status, @Condition,
                     @PurchaseDate, @Cost, @Location, '', @AssignedTo, @AssignedDate)
                """,
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    asset.Tag,
                    asset.Category,
                    asset.Make,
                    asset.Model,
                    asset.Serial,
                    asset.Status,
                    asset.Condition,
                    asset.PurchaseDate,
                    asset.Cost,
                    asset.Location,
                    AssignedTo = asset.AssignedToIndex is { } idx ? employeeIds[idx] : null,
                    asset.AssignedDate,
                },
                tx);
        }

        tx.Commit();
    }
}
