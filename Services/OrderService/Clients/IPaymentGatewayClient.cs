namespace OrderService.Clients;

public record PaymentGatewayOrder(string GatewayOrderId,string KeyId);

public interface IPaymentGatewayClient
{
    Task<PaymentGatewayOrder> CreateOrderAsync(decimal amount, string currency, string receipt);
    bool VerifySignature(string gatewayOrderId, string gatewayPaymentId,string signature);
}