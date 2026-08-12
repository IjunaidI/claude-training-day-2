namespace AssetDesk.Data;

public enum Category { Laptop, Monitor, Headset, Dock, Phone, Keyboard, Other }
public enum Status   { InStock, Assigned, Repair, Retired }
public enum Condition { New, Good, Fair, Poor }

public record Employee(
    string Id,          // Guid.NewGuid().ToString()
    string Name,
    string Email,
    string Department,
    string Title);

public record Asset(
    string Id,
    string Tag,             // unique, format "AST-1001"
    Category Category,
    string Make,            // "Apple"
    string Model,           // "MacBook Pro 14 M3"
    string Serial,
    Status Status,
    Condition Condition,
    string PurchaseDate,    // "2024-03-11", ISO date, no time
    double Cost,            // 2400
    string Location,        // "HQ / Store Room"
    string Notes,           // may be ""
    string? AssignedTo,     // Employee.Id
    string? AssignedDate);  // ISO date

public record AppState(List<Employee> Employees, List<Asset> Assets);

public record NewAssetInput(
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
    int Quantity);          // 1-20

public class AssetDeskException(string message) : Exception(message);
