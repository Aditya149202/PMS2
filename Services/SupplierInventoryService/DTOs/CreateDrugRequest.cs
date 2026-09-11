namespace SupplierInventoryService.DTOs;


public record CreateDrugRequest(string Name, decimal Price, int QuantityInStock, int SupplierId);