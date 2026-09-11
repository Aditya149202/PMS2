using Microsoft.EntityFrameworkCore;
using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Repositories.Implementations;

public class SupplierRepository : ISupplierRepository
{
    private readonly SupplierInventoryDbContext _context;

    public SupplierRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers.ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}