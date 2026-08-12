namespace AssetDesk.Data;

public record EmployeeDto(
    string Id,
    string Name,
    string Email,
    string Department,
    string Title);

public record AssetDto(
    string Id,
    string Tag,
    Category Category,
    string Make,
    string Model,
    string Serial,
    Status Status,
    Condition Condition,
    string PurchaseDate,
    double Cost,
    string Location,
    string Notes,
    string? AssignedTo,
    string? AssignedDate);

public record AppStateDto(List<EmployeeDto> Employees, List<AssetDto> Assets);

public record AssignRequest(string EmployeeId, string AssignedDate);

public record StatusRequest(Status Status);

public record ErrorResponse(string Error);

public static class DtoMapping
{
    public static EmployeeDto ToDto(this Employee e) =>
        new(e.Id, e.Name, e.Email, e.Department, e.Title);

    public static AssetDto ToDto(this Asset a) =>
        new(a.Id, a.Tag, a.Category, a.Make, a.Model, a.Serial, a.Status, a.Condition,
            a.PurchaseDate, a.Cost, a.Location, a.Notes, a.AssignedTo, a.AssignedDate);

    public static AppStateDto ToDto(this AppState s) =>
        new(s.Employees.Select(ToDto).ToList(), s.Assets.Select(ToDto).ToList());
}
