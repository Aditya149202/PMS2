using System.Net.Http.Json;
using OrderService.ExceptionMiddleware;

namespace OrderService.Clients;

public class SupplierInventoryClient : ISupplierInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SupplierInventoryClient> _logger;

    public SupplierInventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    
    

    public async Task CommitSaleAsync(int orderId, int paymentIntentId, decimal totalAmount)
    {
        

        var payload = new { OrderId = orderId, PaymentIntentId = paymentIntentId, Amount = totalAmount };
        var response = await _httpClient.PostAsJsonAsync("/internal/sales/commit", payload);

        if (response.IsSuccessStatusCode) return;

        switch ((int)response.StatusCode)
        {
            case 404:
                throw new NotFoundException($"StockReservation for PaymentIntent {paymentIntentId} not found.");
            case 409:
                throw new StockUnavailableException($"Could not commit sale for order {orderId} — reservation conflict.");
            case 400:
                throw new AppValidationException(new Dictionary<string, string[]>
                    {
                        ["saleCommit"] = new[]
                        {
                            $"Invalid sale-commit request for order {orderId}."
                        }
                    });
            default:
                // Genuinely unexpected (5xx, network-level) — do not swallow. Let the pickup
                // request fail loudly rather than silently marking an order COMPLETED
                // when the Sale record was never actually created.
                response.EnsureSuccessStatusCode();
                break;
        }
    }

    public async Task ReleaseReservationAsync(int paymentIntentId)
    {
        

        var response = await _httpClient.PostAsync($"/internal/reservations/{paymentIntentId}/release", null);

        if (response.IsSuccessStatusCode) return;

        if ((int)response.StatusCode == 404)
        {
            // Reservation already released or never existed — treat as a no-op rather than
            // blocking the cancel. A cancel should always be able to complete on the OrderService
            // side even if inventory-side state is already consistent.
            return;
        }

        response.EnsureSuccessStatusCode();
    }

     public async Task<List<(int DrugId, string DrugName, decimal UnitPrice)>> ReserveStockAsync(
    int paymentIntentId, List<(int DrugId, int Quantity)> items)
{
    var payload = new
    {
        PaymentIntentId = paymentIntentId,
        Items = items.Select(i => new { DrugId = i.DrugId, Quantity = i.Quantity })
    };
    var response = await _httpClient.PostAsJsonAsync("internal/stock/reserve", payload);

    if (!response.IsSuccessStatusCode)
    {
        switch ((int)response.StatusCode)
        {
            case 404:
                throw new NotFoundException("One or more drugs in the order were not found.");
            case 409:
                throw new StockUnavailableException("One or more items are out of stock.");
            case 400:
                throw new AppValidationException(new Dictionary<string, string[]>
                    { ["items"] = new[] { "Invalid stock reservation request." } });
            default:
                response.EnsureSuccessStatusCode();
                break;
        }
    }

    var result = await response.Content.ReadFromJsonAsync<ReserveStockResponseDto>();
    return result!.Items.Select(i => (i.DrugId, i.DrugName, i.UnitPrice)).ToList();
}

    private record ReserveStockResponseDto(List<ReserveStockResponseItemDto> Items);
    private record ReserveStockResponseItemDto(int DrugId, string DrugName, decimal UnitPrice);
}
