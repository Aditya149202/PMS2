using Microsoft.EntityFrameworkCore;
using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Repositories.Implementations;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly SupplierInventoryDbContext _context;

    public ProcessedEventRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(int orderId) =>
        await _context.ProcessedEvents.AnyAsync(e => e.OrderId == orderId);

    public async Task AddAsync(ProcessedEvent processedEvent) =>
        await _context.ProcessedEvents.AddAsync(processedEvent);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}