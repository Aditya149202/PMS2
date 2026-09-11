namespace OrderService.Clients;

public interface ISupplierInventoryClient
{
    // Replaces the old RabbitMQ OrderPickedUp event. Called synchronously from
    // OrderService.PickupOrderAsync. Must be idempotent on the receiving side —
    // reuse the already-locked ProcessedEvents(order_id PK) mechanism there,
    // just triggered by this REST call instead of a queued event.
    Task CommitSaleAsync(int orderId, int paymentIntentId, decimal totalAmount);

    // Called from CancelOrderAsync for NEW/VERIFIED cancels. Restores stock by
    // releasing the ACTIVE StockReservation tied to this PaymentIntent.
    Task<List<(int DrugId, string DrugName, decimal UnitPrice)>> ReserveStockAsync(int paymentIntentId, List<(int DrugId, int Quantity)> items);
    Task ReleaseReservationAsync(int paymentIntentId);
}