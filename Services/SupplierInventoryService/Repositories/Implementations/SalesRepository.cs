using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SupplierInventoryService.Repositories.Implementations;

public class SalesRepository : ISalesRepository
{
    private readonly SupplierInventoryDbContext _context;

    public SalesRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<Sale>> GetByDateRangeAsync(DateTime? from, DateTime? to)
    {
        var query = _context.Sales.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(s => s.SaleDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.SaleDate <= to.Value);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(Sale sale) => await _context.Sales.AddAsync(sale);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}