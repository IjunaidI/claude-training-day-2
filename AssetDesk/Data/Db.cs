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
        var assetCount = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM assets");
        if (assetCount > 0) return;

        var empIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid().ToString()).ToArray();

        using var tx = conn.BeginTransaction();

        var employees = new[]
        {
            new { Id = empIds[0], Name = "Aisha Rahman",   Email = "aisha.rahman@northwind.co",   Department = "Engineering", Title = "Backend Engineer" },
            new { Id = empIds[1], Name = "Daniel Okafor",  Email = "daniel.okafor@northwind.co",  Department = "Design",      Title = "Product Designer" },
            new { Id = empIds[2], Name = "Mei Tanaka",     Email = "mei.tanaka@northwind.co",     Department = "Sales",       Title = "Account Executive" },
            new { Id = empIds[3], Name = "Lucas Moreau",   Email = "lucas.moreau@northwind.co",   Department = "Engineering", Title = "QA Engineer" },
            new { Id = empIds[4], Name = "Priya Nair",     Email = "priya.nair@northwind.co",     Department = "People Ops",  Title = "HR Generalist" },
        };

        conn.Execute(
            "INSERT INTO employees (id, name, email, department, title) VALUES (@Id, @Name, @Email, @Department, @Title)",
            employees, tx);

        var assets = new[]
        {
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1001", Category = "Laptop",   Make = "Apple",    Model = "MacBook Pro 14 M3",       Serial = "C02XK1QF", Status = "assigned", AssignedTo = (string?)empIds[0], AssignedDate = (string?)"2024-04-02", Condition = "good", PurchaseDate = "2024-03-28", Cost = 2400.0, Location = "HQ / Floor 3" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1002", Category = "Laptop",   Make = "Apple",    Model = "MacBook Air 13 M2",       Serial = "C02YT8LM", Status = "assigned", AssignedTo = (string?)empIds[1], AssignedDate = (string?)"2024-06-17", Condition = "good", PurchaseDate = "2024-06-10", Cost = 1350.0, Location = "HQ / Floor 2" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1003", Category = "Laptop",   Make = "Lenovo",   Model = "ThinkPad X1 Carbon G11",  Serial = "PF3K92XA", Status = "in_stock", AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "new",  PurchaseDate = "2025-01-15", Cost = 1800.0, Location = "HQ / Store Room" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1004", Category = "Laptop",   Make = "Lenovo",   Model = "ThinkPad T14 G4",         Serial = "PF2M40BC", Status = "repair",   AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "fair", PurchaseDate = "2023-09-04", Cost = 1200.0, Location = "HQ / IT Bench" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1005", Category = "Monitor",  Make = "Dell",     Model = "UltraSharp U2723QE",      Serial = "CN0TX41A", Status = "assigned", AssignedTo = (string?)empIds[0], AssignedDate = (string?)"2024-04-02", Condition = "good", PurchaseDate = "2024-03-28", Cost = 620.0,  Location = "HQ / Floor 3" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1006", Category = "Monitor",  Make = "Dell",     Model = "UltraSharp U2723QE",      Serial = "CN0TX41B", Status = "in_stock", AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "good", PurchaseDate = "2024-03-28", Cost = 620.0,  Location = "HQ / Store Room" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1007", Category = "Headset",  Make = "Sony",     Model = "WH-1000XM5",              Serial = "S5H88201", Status = "assigned", AssignedTo = (string?)empIds[2], AssignedDate = (string?)"2025-02-03", Condition = "good", PurchaseDate = "2025-01-29", Cost = 380.0,  Location = "HQ / Floor 1" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1008", Category = "Headset",  Make = "Jabra",    Model = "Evolve2 65",              Serial = "JB2065X1", Status = "in_stock", AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "new",  PurchaseDate = "2025-05-20", Cost = 210.0,  Location = "HQ / Store Room" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1009", Category = "Headset",  Make = "Jabra",    Model = "Evolve2 65",              Serial = "JB2065X2", Status = "in_stock", AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "new",  PurchaseDate = "2025-05-20", Cost = 210.0,  Location = "HQ / Store Room" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1010", Category = "Dock",     Make = "CalDigit", Model = "TS4 Thunderbolt",         Serial = "CD4TS091", Status = "assigned", AssignedTo = (string?)empIds[1], AssignedDate = (string?)"2024-06-17", Condition = "good", PurchaseDate = "2024-06-10", Cost = 380.0,  Location = "HQ / Floor 2" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1011", Category = "Phone",    Make = "Apple",    Model = "iPhone 15",               Serial = "F17GK220", Status = "assigned", AssignedTo = (string?)empIds[2], AssignedDate = (string?)"2024-11-11", Condition = "good", PurchaseDate = "2024-11-05", Cost = 900.0,  Location = "HQ / Floor 1" },
            new { Id = Guid.NewGuid().ToString(), Tag = "AST-1012", Category = "Keyboard", Make = "Keychron", Model = "K3 Pro",                  Serial = "KC3P7741", Status = "retired",  AssignedTo = (string?)null,      AssignedDate = (string?)null,          Condition = "poor", PurchaseDate = "2022-08-19", Cost = 95.0,   Location = "HQ / Store Room" },
        };

        conn.Execute("""
            INSERT INTO assets
                (id, tag, category, make, model, serial, status, condition, purchase_date, cost, location, notes, assigned_to, assigned_date)
            VALUES
                (@Id, @Tag, @Category, @Make, @Model, @Serial, @Status, @Condition, @PurchaseDate, @Cost, @Location, '', @AssignedTo, @AssignedDate)
            """,
            assets, tx);

        tx.Commit();
    }
}
