using System.Globalization;

namespace AssetDesk.Data;

public static class Format
{
    public const string Currency = "USD";

    private static readonly Dictionary<Status, (string Db, string Label)> StatusMap = new()
    {
        [Status.InStock] = ("in_stock", "In stock"),
        [Status.Assigned] = ("assigned", "Assigned"),
        [Status.Repair] = ("repair", "In repair"),
        [Status.Retired] = ("retired", "Retired"),
    };

    private static readonly Dictionary<Condition, (string Db, string Label)> ConditionMap = new()
    {
        [Condition.New] = ("new", "New"),
        [Condition.Good] = ("good", "Good"),
        [Condition.Fair] = ("fair", "Fair"),
        [Condition.Poor] = ("poor", "Poor"),
    };

    public static string ToDb(Status status) => StatusMap[status].Db;
    public static string Label(Status status) => StatusMap[status].Label;
    public static Status StatusFromDb(string db) =>
        StatusMap.First(kv => kv.Value.Db == db).Key;

    public static string ToDb(Condition condition) => ConditionMap[condition].Db;
    public static string Label(Condition condition) => ConditionMap[condition].Label;
    public static Condition ConditionFromDb(string db) =>
        ConditionMap.First(kv => kv.Value.Db == db).Key;

    public static string ToDb(Category category) => category.ToString();
    public static string Label(Category category) => category.ToString();
    public static Category CategoryFromDb(string db) => Enum.Parse<Category>(db);

    public static string Money(double amount)
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.CurrencySymbol = Currency == "USD" ? "$" : Currency;
        return amount.ToString("C0", format);
    }

    public static string NextTag(IEnumerable<Asset> assets)
    {
        const string prefix = "AST-";

        var numeric = assets
            .Select(a => a.Tag)
            .Where(t => t.StartsWith(prefix, StringComparison.Ordinal) && t.Length > prefix.Length)
            .Select(t => t[prefix.Length..])
            .Where(s => s.Length > 0 && s.All(char.IsDigit))
            .ToList();

        if (numeric.Count == 0) return prefix + "1001";

        var width = numeric.Max(s => s.Length);
        var next = numeric.Max(long.Parse) + 1;
        return prefix + next.ToString().PadLeft(width, '0');
    }
}
