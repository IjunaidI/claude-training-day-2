using System.Globalization;

namespace AssetDesk.Data;

public static class Format
{
    public const string Currency = "USD";
    private const string DatePattern = "yyyy-MM-dd";

    private static readonly Dictionary<Status, string> StatusToDb = new()
    {
        [Status.InStock] = "in_stock",
        [Status.Assigned] = "assigned",
        [Status.Repair] = "repair",
        [Status.Retired] = "retired",
    };

    private static readonly Dictionary<string, Status> StatusFromDb =
        StatusToDb.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static readonly Dictionary<Status, string> StatusLabels = new()
    {
        [Status.InStock] = "In stock",
        [Status.Assigned] = "Assigned",
        [Status.Repair] = "In repair",
        [Status.Retired] = "Retired",
    };

    private static readonly Dictionary<Condition, string> ConditionToDb = new()
    {
        [Condition.New] = "new",
        [Condition.Good] = "good",
        [Condition.Fair] = "fair",
        [Condition.Poor] = "poor",
    };

    private static readonly Dictionary<string, Condition> ConditionFromDb =
        ConditionToDb.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static readonly Dictionary<Condition, string> ConditionLabels = new()
    {
        [Condition.New] = "New",
        [Condition.Good] = "Good",
        [Condition.Fair] = "Fair",
        [Condition.Poor] = "Poor",
    };

    public static string ToDb(Status status) => StatusToDb[status];
    public static Status StatusFromDbValue(string value) => StatusFromDb[value];
    public static string Label(Status status) => StatusLabels[status];

    public static string ToDb(Condition condition) => ConditionToDb[condition];
    public static Condition ConditionFromDbValue(string value) => ConditionFromDb[value];
    public static string Label(Condition condition) => ConditionLabels[condition];

    public static string ToDb(Category category) => category.ToString();
    public static Category CategoryFromDbValue(string value) => Enum.Parse<Category>(value);
    public static string Label(Category category) => category.ToString();

    public static string Money(double amount) =>
        "$" + amount.ToString("N0", CultureInfo.InvariantCulture);

    public static string Today() => DateTime.Now.ToString(DatePattern, CultureInfo.InvariantCulture);

    public static string ToIsoDate(DateTime date) => date.ToString(DatePattern, CultureInfo.InvariantCulture);

    public static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParseExact(value, DatePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public static bool IsValidDate(string value) => TryParseDate(value, out _);
}
