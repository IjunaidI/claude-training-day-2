using System.Globalization;

namespace AssetDesk.Data;

public static class Format
{
    public const string Currency = "USD";
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["USD"] = "$",
    };

    private static readonly (Status Value, string Db, string Label)[] StatusTable =
    [
        (Status.InStock,  "in_stock", "In stock"),
        (Status.Assigned, "assigned", "Assigned"),
        (Status.Repair,   "repair",   "In repair"),
        (Status.Retired,  "retired",  "Retired"),
    ];

    private static readonly (Condition Value, string Db, string Label)[] ConditionTable =
    [
        (Condition.New,  "new",  "New"),
        (Condition.Good, "good", "Good"),
        (Condition.Fair, "fair", "Fair"),
        (Condition.Poor, "poor", "Poor"),
    ];

    private static readonly (Category Value, string Db, string Label)[] CategoryTable =
        Enum.GetValues<Category>().Select(c => (c, c.ToString(), c.ToString())).ToArray();

    public static string ToDb(Status status) => StatusTable.First(r => r.Value == status).Db;
    public static Status StatusFromDb(string db) => StatusTable.First(r => r.Db == db).Value;
    public static string Label(Status status) => StatusTable.First(r => r.Value == status).Label;

    public static string ToDb(Condition condition) => ConditionTable.First(r => r.Value == condition).Db;
    public static Condition ConditionFromDb(string db) => ConditionTable.First(r => r.Db == db).Value;
    public static string Label(Condition condition) => ConditionTable.First(r => r.Value == condition).Label;

    public static string ToDb(Category category) => CategoryTable.First(r => r.Value == category).Db;
    public static Category CategoryFromDb(string db) => CategoryTable.First(r => r.Db == db).Value;
    public static string Label(Category category) => CategoryTable.First(r => r.Value == category).Label;

    public static string Money(double amount) =>
        $"{CurrencySymbols[Currency]}{amount.ToString("N0", CultureInfo.InvariantCulture)}";

    public static string TodayIso() => DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string ToIsoDate(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static bool TryParseIsoDate(string value, out DateTime date) =>
        DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
