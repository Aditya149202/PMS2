namespace OrderService.Entities;
public class PaymentIntent
{
    public int Id { get; set; }
    public int DoctorId { get; set; }

    public string RazorpayOrderId { get; set; } = default!;
    public string? RazorpayPaymentId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = default!;

    public string ItemsSnapshot { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    
    public Order? Order { get; set; }
}