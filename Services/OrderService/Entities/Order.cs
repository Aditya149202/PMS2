namespace OrderService.Entities;
public class Order
{
    public int Id { get; set; }
    public int PaymentIntentId { get; set; }

    public int DoctorId { get; set; }
    public string DoctorNameSnapshot { get; set; } = default!;

    public string Status { get; set; } = default!;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? CancelledBy { get; set; }

    public PaymentIntent PaymentIntent { get; set; } = default!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}