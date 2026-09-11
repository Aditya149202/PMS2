namespace SupplierInventoryService.Entities;


public class Sale
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime SaleDate { get; set; }
}