namespace SupplierInventoryService.DTOs;
public record UpdateSupplierRequest(string Name, string ContactInfo, string Address, bool IsActive);