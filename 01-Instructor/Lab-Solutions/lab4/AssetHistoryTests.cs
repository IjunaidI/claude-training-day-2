// AssetHistoryTests.cs: reference tests for specs/asset-history.md (Lab 4).
// One [Fact] per acceptance criterion AC-1 to AC-10. AC-11 (curl) and AC-12 (UI) are checked by hand.
//
// These tests compile against any build that uses SPEC.md's exact names plus the names the feature
// spec adds (AssetEventType, AssetEvent, GetHistory). They assume only three things:
//
//   1. The constructor is `new AssetRepository(string connectionString)`.
//      SPEC.md never writes the constructor, but §3.4 says each method opens
//      `new SqliteConnection(_connectionString)` and §3.3 item 2 says the connection string carries
//      `Foreign Keys=True`, so a string in, a string field out is the only shape it describes.
//      If your build takes something else, change the one line in the constructor below.
//
//   2. Nothing about Db.cs. SPEC.md does not name the schema-init method, so this file creates the
//      schema itself from the SQL in SPEC.md §4.5 plus specs/asset-history.md §4.1, copied verbatim.
//      That also checks the implementation against the DDL the feature spec pins. If your build has
//      `Db.Initialize(string connectionString)` you may call it instead; every test still passes,
//      because the tests never depend on seed rows.
//
//   3. The model types live in namespace `AssetDesk.Data` or `AssetDesk`. The empty
//      `namespace AssetDesk.Data { }` below keeps the using directive compiling either way.
//
// Every test gets its own temp SQLite file (Pooling=False so the file can be deleted) and never
// touches AssetDesk/data/assetdesk.db.

using System.Globalization;
using AssetDesk.Data;
using Microsoft.Data.Sqlite;

// Assumption 3: keeps `using AssetDesk.Data;` compiling in a build whose models live in `AssetDesk`.
namespace AssetDesk.Data { }

namespace AssetDesk.Tests
{
    public sealed class AssetHistoryTests : IDisposable
    {
        private const string UnknownIdMessage = "That asset no longer exists. Refresh to see current data.";

        // SPEC.md §4.5 (minus the journal_mode pragma) + specs/asset-history.md §4.1, verbatim.
        private const string SchemaSql = """
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

            CREATE TABLE IF NOT EXISTS asset_events (
              id          INTEGER PRIMARY KEY,
              asset_id    TEXT NOT NULL REFERENCES assets(id),
              type        TEXT NOT NULL CHECK (type IN
                            ('created','assigned','returned','status_changed')),
              from_status TEXT CHECK (from_status IN
                            ('in_stock','assigned','repair','retired')),
              to_status   TEXT NOT NULL CHECK (to_status IN
                            ('in_stock','assigned','repair','retired')),
              employee_id TEXT REFERENCES employees(id),
              occurred_at TEXT NOT NULL,
              CHECK (
                (type =  'created' AND from_status IS NULL     AND to_status = 'in_stock') OR
                (type <> 'created' AND from_status IS NOT NULL AND from_status <> to_status)
              )
            );

            CREATE INDEX IF NOT EXISTS idx_asset_events_asset ON asset_events(asset_id);
            """;

        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly string _employeeId = Guid.NewGuid().ToString();
        private readonly AssetRepository _repo;

        public AssetHistoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"assetdesk-test-{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_dbPath};Foreign Keys=True;Pooling=False";

            Execute(SchemaSql);
            Execute(
                """
                INSERT INTO employees (id, name, email, department, title)
                VALUES ($id, 'Test Person', 'test.person@example.test', 'IT', 'Tester')
                """,
                ("$id", _employeeId));

            _repo = new AssetRepository(_connectionString); // assumption 1
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                File.Delete(_dbPath + suffix);
            }
        }

        [Fact]
        public void AC01_CreateWithQuantity3_RecordsOneCreatedEventPerAsset()
        {
            var created = _repo.CreateAssets(NewInput("AST-2001", quantity: 3));

            Assert.Equal(3, created.Count);
            foreach (var asset in created)
            {
                var history = _repo.GetHistory(asset.Id);

                var e = Assert.Single(history);
                Assert.Equal(asset.Id, e.AssetId);
                Assert.Equal(AssetEventType.Created, e.Type);
                Assert.Null(e.FromStatus);
                Assert.Equal(Status.InStock, e.ToStatus);
                Assert.Null(e.EmployeeId);
                Assert.True(
                    DateTime.TryParseExact(e.OccurredAt, "yyyy-MM-dd'T'HH:mm:ss'Z'",
                        CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _),
                    $"OccurredAt '{e.OccurredAt}' is not yyyy-MM-ddTHH:mm:ssZ");
            }
        }

        [Fact]
        public void AC02_Assign_RecordsAssignedEventWithTheEmployee()
        {
            var asset = CreateOne("AST-2101");

            _repo.Assign(asset.Id, _employeeId, "2026-09-28");

            var newest = _repo.GetHistory(asset.Id)[0];
            Assert.Equal(AssetEventType.Assigned, newest.Type);
            Assert.Equal(Status.InStock, newest.FromStatus);
            Assert.Equal(Status.Assigned, newest.ToStatus);
            Assert.Equal(_employeeId, newest.EmployeeId);
        }

        [Fact]
        public void AC03_AssignThenReturn_AddsTwoEventsNewestFirst()
        {
            var asset = CreateOne("AST-2201");

            _repo.Assign(asset.Id, _employeeId, "2026-09-28");
            _repo.Return(asset.Id);

            var history = _repo.GetHistory(asset.Id);
            Assert.Equal(
                [AssetEventType.Returned, AssetEventType.Assigned, AssetEventType.Created],
                history.Select(e => e.Type).ToArray());
            Assert.True(history[0].Id > history[1].Id && history[1].Id > history[2].Id,
                "event ids must strictly decrease (newest first)");

            var returned = history[0];
            Assert.Equal(Status.Assigned, returned.FromStatus);
            Assert.Equal(Status.InStock, returned.ToStatus);
            Assert.Equal(_employeeId, returned.EmployeeId);
        }

        [Fact]
        public void AC04_SetStatusRepair_RecordsStatusChanged()
        {
            var asset = CreateOne("AST-2301");

            _repo.SetStatus(asset.Id, Status.Repair);

            var newest = _repo.GetHistory(asset.Id)[0];
            Assert.Equal(AssetEventType.StatusChanged, newest.Type);
            Assert.Equal(Status.InStock, newest.FromStatus);
            Assert.Equal(Status.Repair, newest.ToStatus);
            Assert.Null(newest.EmployeeId);
        }

        [Fact]
        public void AC05_RetireAssigned_RecordsExactlyOneStatusChanged()
        {
            var asset = CreateOne("AST-2401");
            _repo.Assign(asset.Id, _employeeId, "2026-09-28");
            var before = _repo.GetHistory(asset.Id).Count;

            var retired = _repo.SetStatus(asset.Id, Status.Retired);

            var history = _repo.GetHistory(asset.Id);
            Assert.Equal(before + 1, history.Count);
            Assert.Equal(AssetEventType.StatusChanged, history[0].Type);
            Assert.Equal(Status.Assigned, history[0].FromStatus);
            Assert.Equal(Status.Retired, history[0].ToStatus);
            Assert.Null(history[0].EmployeeId);
            Assert.Null(retired.AssignedTo);
            Assert.Null(retired.AssignedDate);
        }

        [Fact]
        public void AC06_SetStatusToCurrentStatus_SucceedsAndRecordsNothing()
        {
            var asset = CreateOne("AST-2501");
            _repo.SetStatus(asset.Id, Status.Repair);
            var before = _repo.GetHistory(asset.Id).Count;

            var result = _repo.SetStatus(asset.Id, Status.Repair);

            Assert.Equal(Status.Repair, result.Status);
            Assert.Equal(before, _repo.GetHistory(asset.Id).Count);
        }

        [Fact]
        public void AC07_AssignAssetInRepair_ThrowsAndRecordsNothing()
        {
            var asset = CreateOne("AST-2601");
            _repo.SetStatus(asset.Id, Status.Repair);
            var before = _repo.GetHistory(asset.Id).Count;

            var ex = Assert.Throws<AssetDeskException>(
                () => _repo.Assign(asset.Id, _employeeId, "2026-09-28"));

            Assert.Equal("AST-2601 is in repair and cannot be assigned.", ex.Message);
            Assert.Equal(before, _repo.GetHistory(asset.Id).Count);
        }

        [Fact]
        public void AC08_AssignToFabricatedEmployee_ThrowsAndRecordsNothing()
        {
            var asset = CreateOne("AST-2701");

            var ex = Assert.Throws<AssetDeskException>(
                () => _repo.Assign(asset.Id, Guid.NewGuid().ToString(), "2026-09-28"));

            Assert.Equal(UnknownIdMessage, ex.Message);
            Assert.Equal(Status.InStock, _repo.GetState().Assets.Single(a => a.Id == asset.Id).Status);
            Assert.Single(_repo.GetHistory(asset.Id)); // only Created
        }

        [Fact]
        public void AC09_CreateWithCollidingSecondTag_WritesNoAssetsAndNoEvents()
        {
            CreateOne("AST-2802");
            var assetsBefore = Count("assets");
            var eventsBefore = Count("asset_events");

            var ex = Assert.Throws<AssetDeskException>(
                () => _repo.CreateAssets(NewInput("AST-2801", quantity: 3)));

            Assert.Equal("Tag AST-2802 is already in use.", ex.Message);
            Assert.Equal(assetsBefore, Count("assets"));
            Assert.Equal(eventsBefore, Count("asset_events")); // AST-2801's event was rolled back too
        }

        [Fact]
        public void AC10_History_UnknownIdThrows_KnownIdWithoutEventsIsEmpty()
        {
            var ex = Assert.Throws<AssetDeskException>(() => _repo.GetHistory("no-such-asset"));
            Assert.Equal(UnknownIdMessage, ex.Message);

            // An asset written without the repository, like the seed in Db.cs: no backfill.
            var seededId = Guid.NewGuid().ToString();
            Execute(
                """
                INSERT INTO assets (id, tag, category, make, model, serial, status, condition,
                                    purchase_date, cost, location, notes, assigned_to, assigned_date)
                VALUES ($id, 'AST-2901', 'Laptop', 'Apple', 'MacBook Pro 14 M3', 'C02XK1QF', 'in_stock',
                        'good', '2024-03-28', 2400, 'HQ / Store Room', '', NULL, NULL)
                """,
                ("$id", seededId));

            Assert.Empty(_repo.GetHistory(seededId));
        }

        // ---- helpers -------------------------------------------------------------------------------

        private static NewAssetInput NewInput(string tag, int quantity = 1) => new(
            tag, Category.Laptop, "Lenovo", "ThinkPad T14 G4", $"SN-{tag}", Condition.New,
            "2025-01-15", 1200, "HQ / Store Room", "", quantity);

        private Asset CreateOne(string tag) => Assert.Single(_repo.CreateAssets(NewInput(tag)));

        private long Count(string table)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
            return (long)cmd.ExecuteScalar()!;
        }

        private void Execute(string sql, params (string Name, object Value)[] parameters)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.ExecuteNonQuery();
        }
    }
}
