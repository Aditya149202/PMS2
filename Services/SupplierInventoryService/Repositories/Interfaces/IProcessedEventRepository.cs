using SupplierInventoryService.Entities;

namespace SupplierInventoryService.Repositories.Interfaces;

public interface IProcessedEventRepository
{
    // order_id is the PK — this is the idempotency check itself, not a generic lookup.
    Task<bool> ExistsAsync(int orderId);
    Task AddAsync(ProcessedEvent processedEvent);
    Task SaveChangesAsync();
}