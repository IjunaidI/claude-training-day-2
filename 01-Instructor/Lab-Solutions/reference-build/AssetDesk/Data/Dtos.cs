namespace AssetDesk.Data;

// The boundary's shape, declared once. Components never see the records in Models.cs.
public record EmployeeDto(string Id, string Name, string Email, string Department, string Title);

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

public record NewAssetInputDto(
    string Tag,
    Category Category,
    string Make,
    string Model,
    string Serial,
    Condition Condition,
    string PurchaseDate,
    double Cost,
    string Location,
    string Notes,
    int Quantity);

public record AssignRequest(string EmployeeId, string AssignedDate);
public record StatusRequest(Status Status);
public record ApiError(string Error);
