using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Repositories.Implementations;

public class SalesRepository : ISalesRepository
{
    private readonly SupplierInventoryDbContext _context;

    public SalesRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Sale sale) => await _context.Sales.AddAsync(sale);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}