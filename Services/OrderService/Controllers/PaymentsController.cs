using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;
using System.Text;
using System.Security.Cryptography;

namespace OrderService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentsController(IPaymentService paymentService,IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration=configuration;
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
    //TO BE REMOVED: dummy signature generator
    [HttpGet("test-signature")]
    public IActionResult TestSignature(string orderId, string paymentId)
    {
        var payload = $"{orderId}|{paymentId}";
        var secret = _configuration["Razorpay:KeySecret"]!;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

        var signature = Convert.ToHexString(hash).ToLowerInvariant();

        return Ok(signature);
    }
}