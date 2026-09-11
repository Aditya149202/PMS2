namespace OrderService.DTOs;

public record PaymentConfirmRequest(
    string RazorpayOrderId,
    string RazorpayPaymentId,
    
    string RazorpaySignature);