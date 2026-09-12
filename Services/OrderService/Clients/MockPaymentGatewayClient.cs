namespace OrderService.Clients;

public class MockPaymentGatewayClient : IPaymentGatewayClient
{
    public Task<PaymentGatewayOrder> CreateOrderAsync(decimal amount, string currency, string receipt)
    {
        // Simulate creating an order in the payment gateway
        var mockOrderId = $"mock_order_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentGatewayOrder(mockOrderId, "mock_key_id"));
    }
    public bool VerifySignature(string gatewayOrderId, string gatewayPaymentId, string signature)
    {
        // Simulate signature verification
        return true; // Always return true for the mock implementation
    }
}