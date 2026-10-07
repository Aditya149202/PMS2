using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Enums;
using OrderService.ExceptionMiddleware;
using OrderService.Repositories.Filters;
using OrderService.Repositories.Interfaces;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ISupplierInventoryClient _supplierInventoryClient;

    private readonly IPaymentGatewayClient _paymentGatewayClient;

    public OrderService(IOrderRepository orderRepository, ISupplierInventoryClient supplierInventoryClient)
    {
        _orderRepository = orderRepository;
        _supplierInventoryClient = supplierInventoryClient;
        _paymentGatewayClient = paymentGatewayClient;
    }

    public async Task<PagedResponse<OrderListItemResponse>> GetOrdersAsync(OrderFilterRequest request)
    {
        var filter = new OrderFilter
        {
            DoctorId = request.DoctorId,
            Status = request.Status,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            DrugId = request.DrugId
        };

        var (items, totalCount) = await _orderRepository.GetFilteredAsync(filter, request.Page, request.Size);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.Size);

        return new PagedResponse<OrderListItemResponse>(
            items.Select(ToListItemResponse).ToList(),
            request.Page,
            request.Size,
            totalCount,
            totalPages);
    }

    // requestingDoctorId is null for Admin callers (no ownership check), and set to the
    // caller's own id for Doctor callers. Mismatched ownership throws the SAME NotFoundException
    // as a genuinely missing id — anti-enumeration, same pattern already locked for inactive drugs.
    public async Task<OrderResponse> GetOrderByIdAsync(int id, int? requestingDoctorId)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Order {id} not found.");

        if (requestingDoctorId.HasValue && order.DoctorId != requestingDoctorId.Value)
            throw new NotFoundException($"Order {id} not found.");

        return ToOrderResponse(order);
    }

    public async Task<OrderResponse> VerifyOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Order {id} not found.");

        if (order.Status != OrderStatus.NEW.ToString())
            throw new InvalidOrderStateException($"Order {id} cannot be verified from status {order.Status}. Only NEW orders can be verified.");

        order.Status = OrderStatus.VERIFIED.ToString();
        order.VerifiedAt = DateTime.UtcNow;
        await _orderRepository.SaveChangesAsync();

        return ToOrderResponse(order);
    }

    // Collapsed pickup+complete per the synchronous-architecture decision. If CommitSaleAsync
    // throws, the status mutation below never runs — order stays VERIFIED, safe to retry.
    // If CommitSaleAsync succeeds but SaveChangesAsync then fails, the Sale record already
    // exists on the inventory side and a retry will call CommitSaleAsync again — this is
    // exactly why the ProcessedEvents idempotency table still matters even without RabbitMQ.
    public async Task<OrderResponse> PickupOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Order {id} not found.");

        if (order.Status != OrderStatus.VERIFIED.ToString())
            throw new InvalidOrderStateException($"Order {id} cannot be pickedup from status {order.Status}. Only verified orders can be pickeup.");
        if (order.RefundId is not null)
    throw new InvalidOrderStateException($"Order {id} has been refunded and is awaiting cancellation.");
        await _supplierInventoryClient.CommitSaleAsync(order.Id, order.PaymentIntentId, order.TotalAmount);

        order.Status = OrderStatus.COMPLETED.ToString();
        order.CompletedAt = DateTime.UtcNow;
        await _orderRepository.SaveChangesAsync();

        return ToOrderResponse(order);
    }

    // Stock-restore only. Does NOT reverse the Razorpay charge — see flag above.
    public async Task<OrderResponse> CancelOrderAsync(int id, CancelledBy cancelledBy, int? requestingDoctorId)
{
    var order = await _orderRepository.GetByIdAsync(id)
        ?? throw new NotFoundException($"Order {id} not found.");

    // Anti-enumeration — same NotFoundException as a genuinely missing id, same
    // convention as GetOrderByIdAsync, so a Doctor can't distinguish "not mine" from "doesn't exist."
    if (requestingDoctorId.HasValue && order.DoctorId != requestingDoctorId.Value)
        throw new NotFoundException($"Order {id} not found.");

    // If you go with "Doctor can only cancel pre-verification":
    var allowedStatuses = cancelledBy == CancelledBy.DOCTOR
        ? new[] { OrderStatus.NEW }
        : new[] { OrderStatus.NEW, OrderStatus.VERIFIED };

    if (!allowedStatuses.Contains(Enum.Parse<OrderStatus>(order.Status, ignoreCase: true)))
        throw new InvalidOrderStateException($"Order {id} cannot be cancelled from status {order.Status} by {cancelledBy}.");

        // Refund first and persist the refund id right away. A failure later in this method then leaves
    // a retry-safe state: the retry sees RefundId and does not refund again. A refund failure here
    // throws before anything has changed.
    if (order.RefundId is null)
    {
        var paymentId = order.PaymentIntent.RazorpayPaymentId
            ?? throw new InvalidOperationException($"Order {id} has no Razorpay payment id to refund.");

        var refund = await _paymentGatewayClient.RefundAsync(paymentId, order.TotalAmount, $"refund_order_{order.Id}");
        order.RefundId = refund.RefundId;
        order.RefundStatus = Enum.TryParse<RefundStatus>(refund.Status, true, out var rs)
            ? rs.ToString()
            : RefundStatus.PENDING.ToString();
        await _orderRepository.SaveChangesAsync();
    }

    await _supplierInventoryClient.ReleaseReservationAsync(order.PaymentIntentId);

    order.Status = OrderStatus.CANCELLED.ToString();
    order.CancelledAt = DateTime.UtcNow;
    order.CancelledBy = cancelledBy.ToString();
    await _orderRepository.SaveChangesAsync();

    return ToOrderResponse(order);
}



    private static OrderResponse ToOrderResponse(Order o) => new(
        o.Id, o.DoctorId, o.DoctorNameSnapshot, Enum.Parse<OrderStatus>(o.Status, ignoreCase: true), o.TotalAmount, o.CreatedAt,
        o.VerifiedAt, o.CompletedAt, o.CancelledAt, o.CancelledBy is null
        ? null
        : Enum.Parse<CancelledBy>(o.CancelledBy, ignoreCase: true),
        o.OrderItems.Select(i => new OrderItemResponse(i.DrugId, i.Quantity, i.UnitPriceAtOrder)).ToList()
            ,
    o.RefundId,
    o.RefundStatus is null ? null : Enum.Parse<RefundStatus>(o.RefundStatus, ignoreCase: true));

    private static OrderListItemResponse ToListItemResponse(Order o) => new(
        o.Id, o.DoctorId, o.DoctorNameSnapshot, Enum.Parse<OrderStatus>(o.Status, ignoreCase: true), o.TotalAmount, o.CreatedAt);
}