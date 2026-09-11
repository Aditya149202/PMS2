using Microsoft.EntityFrameworkCore;
using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Repositories.Implementations;

public class DrugRepository : IDrugRepository
{
    private readonly SupplierInventoryDbContext _context;

    public DrugRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Drug drug)
    {
        await _context.Drugs.AddAsync(drug);
    }

    public async Task<IEnumerable<Drug>> GetAllAsync()
    {
        return await _context.Drugs.ToListAsync();
    }

    public async Task<Drug?> GetByIdAsync(int id)
    {
        return await _context.Drugs
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}