using OrderService.DTOs;
using OrderService.Enums;

namespace OrderService.Services;

public interface IOrderEmailService
{
    // Best-effort: never throws. A mail failure must not fail a payment that already succeeded.
    Task SendOrderConfirmationAsync(string? toEmail, string doctorName, int orderId, decimal totalAmount);
}