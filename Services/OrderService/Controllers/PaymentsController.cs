using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "DOCTOR")]
    public async Task<ActionResult<PaymentInitiateResponse>> Initiate([FromBody] PaymentInitiateRequest request)
    {
        var doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var doctorName = User.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("JWT is missing the required name claim.");

        return Ok(await _paymentService.InitiatePaymentAsync(doctorId, doctorName, request));
    }

    [HttpPost("confirm")]
    [Authorize(Roles = "DOCTOR")]
    public async Task<ActionResult<PaymentStatusResponse>> ConfirmPayment([FromBody] PaymentConfirmRequest request)
    {
        var doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var doctorName = User.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("JWT is missing the required name claim.");

        
        return Ok(await _paymentService.ConfirmPaymentAsync(doctorId,doctorName, request));
    }

    [HttpGet("{id}/status")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(int id)
    {
        var requestingDoctorId = User.IsInRole("ADMIN") ? (int?)null : int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _paymentService.GetPaymentStatusAsync(id, requestingDoctorId));
    }
}