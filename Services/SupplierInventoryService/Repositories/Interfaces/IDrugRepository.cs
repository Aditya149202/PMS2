using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;

namespace SupplierInventoryService.Repositories.Interfaces;

public interface IDrugRepository
{
    Task AddAsync(Drug drug);
    Task<IEnumerable<Drug>> GetAllAsync(); // admin CRUD listing
    Task<Drug?> GetByIdAsync(int id);
    Task SaveChangesAsync();
    // "Delete" = set IsActive = false on a tracked entity, then SaveChangesAsync — no method needed here either.
}