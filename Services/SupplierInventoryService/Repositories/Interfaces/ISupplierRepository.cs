using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
namespace SupplierInventoryService.Repositories.Interfaces;
public interface ISupplierRepository
{
    Task AddAsync(Supplier supplier);
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task SaveChangesAsync();
    // No Delete() here — deletion is a service-layer decision (block if
    // referenced by any Drug), not a plain repo method. Update via
    // tracked-entity mutation, same pattern as UserRepository.
}