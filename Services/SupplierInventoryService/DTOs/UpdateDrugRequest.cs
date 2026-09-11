namespace SupplierInventoryService.DTOs;
public record UpdateDrugRequest(string Name, decimal Price, int QuantityInStock, bool IsActive);