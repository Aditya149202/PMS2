namespace SupplierInventoryService.Entities;

public class Drug
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }

    public int QuantityInStock { get; set; }

    public bool IsActive { get; set; }

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = default!;

    public ICollection<StockReservation> StockReservations { get; set; } = new List<StockReservation>();
}