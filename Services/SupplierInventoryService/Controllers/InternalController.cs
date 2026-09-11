using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierInventoryService.DTOs;
using SupplierInventoryService.Services.Interfaces;
using SupplierInventoryService.Enums;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.Services;
using SupplierInventoryService.Auth;
namespace SupplierInventoryService.Controllers;


[ApiController]
[Route("internal")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.SchemeName)]
public class InternalController : ControllerBase
{

    private readonly IInternalService _internalService;

    public InternalController(IInternalService internalService)
    {
        _internalService = internalService;
    }
    [HttpPost("sales/commit")]
    public async Task<IActionResult> CommitSale([FromBody] CommitSaleRequest request)
    {
        
        await _internalService.CommitSaleAsync(request.OrderId, request.PaymentIntentId, request.Amount);
        return Ok();
    }
    [HttpPost("stock/reserve")]
    public async Task<IActionResult> ReserveStock([FromBody] ReserveStockRequest request)
    {
        var items = request.Items.Select(i => (i.DrugId, i.Quantity)).ToList();
        var result = await _internalService.ReserveStockAsync(request.PaymentIntentId, items);
        return Ok(new ReserveStockResponse (
            result.Select(r => new ReserveStockResponseItem ( r.DrugId, r.DrugName,  r.UnitPrice )).ToList() 
            ));
    }
    [HttpPost("reservations/{paymentIntentId}/release")]
    public async Task<IActionResult> ReleaseReservation(int paymentIntentId)
    {
        await _internalService.ReleaseReservationAsync(paymentIntentId);
        return Ok();
    }
}