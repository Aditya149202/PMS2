namespace SupplierInventoryService.DTOs;
public record DrugResponse(int Id, string Name, decimal Price, int QuantityInStock, bool IsActive, int SupplierId);