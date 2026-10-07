namespace OrderService.Clients;

public record PaymentGatewayOrder(string GatewayOrderId,string KeyId);
public record PaymentGatewayRefund(string RefundId, string Status);

// inside the interface:

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayOrder> CreateOrderAsync(decimal amount, string currency, string receipt);
    bool VerifySignature(string gatewayOrderId, string gatewayPaymentId,string signature);
    Task<PaymentGatewayRefund> RefundAsync(string gatewayPaymentId, decimal amount, string receipt);
}