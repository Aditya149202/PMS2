namespace OrderService.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    public int DrugId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceAtOrder { get; set; }

    public Order Order { get; set; } = default!;
}