namespace SupplierInventoryService.Entities;
public class StockReservation
{
    public int Id { get; set; }

    public int PaymentIntentId { get; set; }

    public int DrugId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = default!;

    public DateTime ReservedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public Drug Drug { get; set; } = default!;
}