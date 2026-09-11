using OrderService.DTOs;
using OrderService.Enums;

namespace OrderService.Services;

public interface IOrderService
{
    Task<PagedResponse<OrderListItemResponse>> GetOrdersAsync(OrderFilterRequest request);
    Task<OrderResponse> GetOrderByIdAsync(int id, int? requestingDoctorId);
    Task<OrderResponse> VerifyOrderAsync(int id);
    Task<OrderResponse> PickupOrderAsync(int id);
    Task<OrderResponse> CancelOrderAsync(int id, CancelledBy cancelledBy, int? requestingDoctorId);
}