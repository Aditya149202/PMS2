
using SupplierInventoryService.DTOs;
namespace SupplierInventoryService.Services.Interfaces;

public interface ISupplierService
{
    Task<SupplierResponse> CreateSupplierAsync(CreateSupplierRequest request);
    Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync();
    Task<SupplierResponse> GetSupplierByIdAsync(int id);
    Task<SupplierResponse> UpdateSupplierAsync(int id, UpdateSupplierRequest request);
    Task DeactivateSupplierAsync(int id);
}
