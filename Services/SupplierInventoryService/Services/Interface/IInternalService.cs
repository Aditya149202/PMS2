namespace SupplierInventoryService.Services;

public interface IInternalService
{
    Task<List<(int DrugId,string DrugName,decimal UnitPrice)>> ReserveStockAsync(int paymentIntentId, List<(int DrugId, int Quantity)> items);
    Task CommitSaleAsync(int orderId, int paymentIntentId, decimal amount);
    Task ReleaseReservationAsync(int paymentIntentId);
}