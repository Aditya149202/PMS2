using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Enums;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
[Authorize] // default: any authenticated role, same split pattern as DrugsController
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // Doctor callers are force-scoped to their own DoctorId regardless of what's in the
    // query string — prevents a Doctor from viewing another doctor's orders by editing
    // ?doctorId=. Admin callers get whatever DoctorId filter they passed (or none).
    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderListItemResponse>>> GetOrders([FromQuery] OrderFilterRequest request)
    {
        if (!User.IsInRole("ADMIN"))
            request = request with { DoctorId = GetCallerId() };

        return Ok(await _orderService.GetOrdersAsync(request));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(int id)
    {
        var requestingDoctorId = User.IsInRole("ADMIN") ? (int?)null : GetCallerId();
        return Ok(await _orderService.GetOrderByIdAsync(id, requestingDoctorId));
    }

    [HttpPut("{id}/verify")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<OrderResponse>> VerifyOrder(int id)
        => Ok(await _orderService.VerifyOrderAsync(id));

    [HttpPut("{id}/pickup")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<OrderResponse>> PickupOrder(int id)
        => Ok(await _orderService.PickupOrderAsync(id));

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<OrderResponse>> CancelOrder(int id)
    {
        var isAdmin = User.IsInRole("ADMIN");
        var cancelledBy = isAdmin ? CancelledBy.ADMIN : CancelledBy.DOCTOR;
        var requestingDoctorId = isAdmin ? (int?)null : GetCallerId();

        return Ok(await _orderService.CancelOrderAsync(id, cancelledBy, requestingDoctorId));

    }

    // NOTE: assumes the "sub" claim maps to ClaimTypes.NameIdentifier, matching the
    // ASP.NET Core default JWT claim-type mapping. Verify this against whatever
    // TokenService.GenerateAccessToken actually issues — if that mapping was cleared
    // anywhere in AddJwtBearer setup, this throws instead of resolving the caller.
    private int GetCallerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

