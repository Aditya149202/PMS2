using System.Security.Cryptography;
using System.Text;

using Razorpay.Api;
using RazorpayOrder = Razorpay.Api.Order;

namespace OrderService.Clients;

public class RazorpayGatewayClient : IPaymentGatewayClient
{
    private readonly IConfiguration _config;
    public RazorpayGatewayClient(IConfiguration config)
    {
        _config = config;
    }

    public Task<PaymentGatewayOrder> CreateOrderAsync(decimal amount, string currency, string receiptId)
    {
        var client = new RazorpayClient(_config["Razorpay:KeyId"], _config["Razorpay:KeySecret"]);
        var options = new Dictionary<string, object>
        {
            { "amount", (int)(amount * 100) }, // Amount in paise
            { "currency", currency },
            { "receipt", receiptId }
        };
        RazorpayOrder order = client.Order.Create(options);

        return Task.FromResult(new PaymentGatewayOrder
        (
            order["Id"].ToString(),_config["Razorpay:KeyId"]!));
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var payload = $"{orderId}|{paymentId}";
        var secret = _config["Razorpay:KeySecret"]!;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computedSignature), Encoding.UTF8.GetBytes(signature));
    }

}