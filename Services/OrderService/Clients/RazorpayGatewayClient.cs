using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OrderService.ExceptionMiddleware;
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
        var client = new RazorpayClient(_config["Razorpay:KeyId"].Trim(), _config["Razorpay:KeySecret"].Trim());
        var keyId = _config["Razorpay:KeyId"];
var keySecret = _config["Razorpay:KeySecret"];

      var options = new Dictionary<string, object>
        {
            { "amount", (int)(amount * 100) }, // Amount in paise
            { "currency", currency },
            { "receipt", receiptId }
        };
        RazorpayOrder order = client.Order.Create(options);
        Console.WriteLine(order);
        return Task.FromResult(new PaymentGatewayOrder
        (
            order["id"].ToString(),_config["Razorpay:KeyId"]!));
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
    private static readonly HttpClient Http = new()
{
    BaseAddress = new Uri("https://api.razorpay.com/v1/"),
    Timeout = TimeSpan.FromSeconds(20)
};

public async Task<PaymentGatewayRefund> RefundAsync(string gatewayPaymentId, decimal amount, string receipt)
{
    var paise = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    var payment = await SendAsync(HttpMethod.Get, $"payments/{gatewayPaymentId}");

    // Retry safety: if an earlier attempt refunded but we crashed before saving the id, return that refund.
    var alreadyRefunded = payment.TryGetProperty("amount_refunded", out var ar) ? ar.GetInt64() : 0;
    if (alreadyRefunded >= paise)
    {
        var existing = await SendAsync(HttpMethod.Get, $"payments/{gatewayPaymentId}/refunds");
        var first = existing.GetProperty("items").EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Object)
            return new PaymentGatewayRefund(
                first.GetProperty("id").GetString()!,
                first.GetProperty("status").GetString()!.ToUpperInvariant());
    }

    var status = payment.GetProperty("status").GetString();
    if (status != "captured")
        throw new RefundFailedException($"Payment {gatewayPaymentId} is '{status}', not captured, so it cannot be refunded.");

    var refund = await SendAsync(HttpMethod.Post, $"payments/{gatewayPaymentId}/refund",
        new { amount = paise, speed = "normal", receipt });

    return new PaymentGatewayRefund(
        refund.GetProperty("id").GetString()!,
        refund.GetProperty("status").GetString()!.ToUpperInvariant());
}

private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body = null)
{
    var keyId = _config["Razorpay:KeyId"]!.Trim();
    var keySecret = _config["Razorpay:KeySecret"]!.Trim();

    using var request = new HttpRequestMessage(method, path);
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}")));
    if (body is not null)
        request.Content = JsonContent.Create(body);

    HttpResponseMessage response;
    try
    {
        response = await Http.SendAsync(request);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        throw new RefundFailedException("Could not reach the payment gateway to process the refund.");
    }

    using (response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string? description = null;
            try
            {
                using var err = JsonDocument.Parse(json);
                description = err.RootElement.GetProperty("error").GetProperty("description").GetString();
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { }

            throw new RefundFailedException($"The payment gateway rejected the request: {description ?? response.ReasonPhrase}");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}

}