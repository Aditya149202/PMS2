using SupplierInventoryService.Entities;

namespace SupplierInventoryService.Repositories.Interfaces;

public interface ISalesRepository
{
    // Does NOT call SaveChangesAsync internally — same convention as every other repo here.
    Task AddAsync(Sale sale);

    Task<List<Sale>> GetByDateRangeAsync(DateTime? from, DateTime? to);
    Task SaveChangesAsync();
}