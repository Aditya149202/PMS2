using SupplierInventoryService.Data;

using SupplierInventoryService.DTOs;
namespace SupplierInventoryService.Services.Interfaces;

public interface IDrugService
{
    Task<DrugResponse> CreateDrugAsync(CreateDrugRequest request);
    Task<IEnumerable<DrugResponse>> GetAllDrugsAsync(bool includeInactive);
    Task<DrugResponse> GetDrugByIdAsync(int id,bool includeInactive);
    Task<DrugResponse> UpdateDrugAsync(int id, UpdateDrugRequest request);
    Task DeactivateDrugAsync(int id);
}