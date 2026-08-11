using Dapper;
using Microsoft.Data.Sqlite;

namespace AssetDesk.Data;

public static class Db
{
    private const string Schema = """
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

    public static string ResolveConnectionString(string contentRootPath)
    {
        var dataDir = Path.Combine(contentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "assetdesk.db");
        return $"Data Source={dbPath};Foreign Keys=True";
    }

    public static void Initialize(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        conn.Execute("PRAGMA journal_mode = WAL;");
        conn.Execute(Schema);

        var assetCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM assets");
        if (assetCount == 0)
        {
            Seed(conn);
        }
    }

    private static void Seed(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();

        var employeeIds = new[]
        {
            Guid.NewGuid().ToString(), // emp-1 Aisha Rahman
            Guid.NewGuid().ToString(), // emp-2 Daniel Okafor
            Guid.NewGuid().ToString(), // emp-3 Mei Tanaka
            Guid.NewGuid().ToString(), // emp-4 Lucas Moreau
            Guid.NewGuid().ToString(), // emp-5 Priya Nair
        };

        conn.Execute(
            "INSERT INTO employees (id, name, email, department, title) " +
            "VALUES (@Id, @Name, @Email, @Department, @Title)",
            new[]
            {
                new { Id = employeeIds[0], Name = "Aisha Rahman",   Email = "aisha.rahman@northwind.co",   Department = "Engineering", Title = "Backend Engineer" },
                new { Id = employeeIds[1], Name = "Daniel Okafor",  Email = "daniel.okafor@northwind.co",  Department = "Design",      Title = "Product Designer" },
                new { Id = employeeIds[2], Name = "Mei Tanaka",     Email = "mei.tanaka@northwind.co",     Department = "Sales",       Title = "Account Executive" },
                new { Id = employeeIds[3], Name = "Lucas Moreau",   Email = "lucas.moreau@northwind.co",   Department = "Engineering", Title = "QA Engineer" },
                new { Id = employeeIds[4], Name = "Priya Nair",     Email = "priya.nair@northwind.co",     Department = "People Ops",  Title = "HR Generalist" },
            },
            tx);

        const string insertAsset =
            "INSERT INTO assets (id, tag, category, make, model, serial, status, condition, " +
            "purchase_date, cost, location, notes, assigned_to, assigned_date) " +
            "VALUES (@Id, @Tag, @Category, @Make, @Model, @Serial, @Status, @Condition, " +
            "@PurchaseDate, @Cost, @Location, @Notes, @AssignedTo, @AssignedDate)";

        var assets = new[]
        {
            SeedAsset("AST-1001", Category.Laptop,   "Apple",    "MacBook Pro 14 M3",       "C02XK1QF", Status.Assigned, employeeIds[0], "2024-04-02", Condition.Good, "2024-03-28", 2400, "HQ / Floor 3"),
            SeedAsset("AST-1002", Category.Laptop,   "Apple",    "MacBook Air 13 M2",       "C02YT8LM", Status.Assigned, employeeIds[1], "2024-06-17", Condition.Good, "2024-06-10", 1350, "HQ / Floor 2"),
            SeedAsset("AST-1003", Category.Laptop,   "Lenovo",   "ThinkPad X1 Carbon G11",  "PF3K92XA", Status.InStock,  null,           null,         Condition.New,  "2025-01-15", 1800, "HQ / Store Room"),
            SeedAsset("AST-1004", Category.Laptop,   "Lenovo",   "ThinkPad T14 G4",         "PF2M40BC", Status.Repair,   null,           null,         Condition.Fair, "2023-09-04", 1200, "HQ / IT Bench"),
            SeedAsset("AST-1005", Category.Monitor,  "Dell",     "UltraSharp U2723QE",      "CN0TX41A", Status.Assigned, employeeIds[0], "2024-04-02", Condition.Good, "2024-03-28", 620,  "HQ / Floor 3"),
            SeedAsset("AST-1006", Category.Monitor,  "Dell",     "UltraSharp U2723QE",      "CN0TX41B", Status.InStock,  null,           null,         Condition.Good, "2024-03-28", 620,  "HQ / Store Room"),
            SeedAsset("AST-1007", Category.Headset,  "Sony",     "WH-1000XM5",              "S5H88201", Status.Assigned, employeeIds[2], "2025-02-03", Condition.Good, "2025-01-29", 380,  "HQ / Floor 1"),
            SeedAsset("AST-1008", Category.Headset,  "Jabra",    "Evolve2 65",              "JB2065X1", Status.InStock,  null,           null,         Condition.New,  "2025-05-20", 210,  "HQ / Store Room"),
            SeedAsset("AST-1009", Category.Headset,  "Jabra",    "Evolve2 65",              "JB2065X2", Status.InStock,  null,           null,         Condition.New,  "2025-05-20", 210,  "HQ / Store Room"),
            SeedAsset("AST-1010", Category.Dock,     "CalDigit", "TS4 Thunderbolt",         "CD4TS091", Status.Assigned, employeeIds[1], "2024-06-17", Condition.Good, "2024-06-10", 380,  "HQ / Floor 2"),
            SeedAsset("AST-1011", Category.Phone,    "Apple",    "iPhone 15",               "F17GK220", Status.Assigned, employeeIds[2], "2024-11-11", Condition.Good, "2024-11-05", 900,  "HQ / Floor 1"),
            SeedAsset("AST-1012", Category.Keyboard, "Keychron", "K3 Pro",                  "KC3P7741", Status.Retired,  null,           null,         Condition.Poor, "2022-08-19", 95,   "HQ / Store Room"),
        };

        conn.Execute(insertAsset, assets, tx);

        tx.Commit();
    }

    private static object SeedAsset(
        string tag, Category category, string make, string model, string serial,
        Status status, string? assignedTo, string? assignedDate, Condition condition,
        string purchaseDate, double cost, string location) => new
        {
            Id = Guid.NewGuid().ToString(),
            Tag = tag,
            Category = Format.ToDb(category),
            Make = make,
            Model = model,
            Serial = serial,
            Status = Format.ToDb(status),
            Condition = Format.ToDb(condition),
            PurchaseDate = purchaseDate,
            Cost = cost,
            Location = location,
            Notes = "",
            AssignedTo = assignedTo,
            AssignedDate = assignedDate,
        };
}
