using Moq;
using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Enums;
using OrderService.ExceptionMiddleware;
using OrderService.Repositories.Interfaces;
using OrderService.Services;

namespace OrderService.Tests.Services;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentIntentRepository> _paymentIntentRepo;
    private Mock<IOrderRepository> _orderRepo;
    private Mock<ISupplierInventoryClient> _supplierInventoryClient;
    private Mock<IPaymentGatewayClient> _paymentGatewayClient;
    private PaymentService _sut;

    [SetUp]
    public void Setup()
    {
        _paymentIntentRepo = new Mock<IPaymentIntentRepository>();
        _orderRepo = new Mock<IOrderRepository>();
        _supplierInventoryClient = new Mock<ISupplierInventoryClient>();
        _paymentGatewayClient = new Mock<IPaymentGatewayClient>();

        _sut = new PaymentService(
            _paymentIntentRepo.Object,
            _orderRepo.Object,
            _supplierInventoryClient.Object,
            _paymentGatewayClient.Object);
    }

    [Test]
    public void InitiatePaymentAsync_Should_Throw_AppValidationException_When_No_Items()
    {
        var request = new PaymentInitiateRequest(new List<PaymentInitiateRequestItem>());

        Assert.ThrowsAsync<AppValidationException>(
            () => _sut.InitiatePaymentAsync(doctorId: 1, doctorName: "Dr. Test", request));
    }

    // This is the actual point of the abstraction: PaymentService never knows or cares
    // whether IPaymentGatewayClient is the Mock or the real Razorpay implementation —
    // it only calls the interface. This test proves that boundary holds.
    [Test]
    public async Task InitiatePaymentAsync_Should_Only_Depend_On_IPaymentGatewayClient_Abstraction()
    {
        var request = new PaymentInitiateRequest(new List<PaymentInitiateRequestItem> { new(DrugId: 1, Quantity: 2) });

        _supplierInventoryClient
            .Setup(c => c.ReserveStockAsync(It.IsAny<int>(), It.IsAny<List<(int DrugId, int Quantity)>>()))
            .ReturnsAsync(new List<(int, string, decimal)> { (1, "Paracetamol", 10m) });

        _paymentGatewayClient
            .Setup(g => g.CreateOrderAsync(It.IsAny<decimal>(), "INR", It.IsAny<string>()))
            .ReturnsAsync(new PaymentGatewayOrder("gw_order_123", "gw_key_abc"));

        var result = await _sut.InitiatePaymentAsync(doctorId: 1, doctorName: "Dr. Test", request);

        Assert.That(result.RazorpayOrderId, Is.EqualTo("gw_order_123"));
        Assert.That(result.RazorpayKeyId, Is.EqualTo("gw_key_abc"));
        Assert.That(result.Amount, Is.EqualTo(20m)); // 2 * 10
        _paymentGatewayClient.Verify(g => g.CreateOrderAsync(20m, "INR", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void ConfirmPaymentAsync_Should_Throw_InvalidPaymentSignatureException_When_Signature_Invalid()
    {
        _paymentGatewayClient
            .Setup(g => g.VerifySignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var request = new PaymentConfirmRequest("gw_order_123", "gw_pay_456", "bad_signature");

        Assert.ThrowsAsync<InvalidPaymentSignatureException>(
            () => _sut.ConfirmPaymentAsync(doctorId: 1, doctorName: "Dr. Test", request));

        // Must fail BEFORE any lookup happens — signature check is the first gate.
        _paymentIntentRepo.Verify(r => r.GetByRazorpayOrderIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ConfirmPaymentAsync_Should_Be_Idempotent_When_Already_Paid()
    {
        _paymentGatewayClient
            .Setup(g => g.VerifySignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var existingOrder = new Order { Id = 99 };
        var paymentIntent = new PaymentIntent
        {
            Id = 5,
            DoctorId = 1,
            RazorpayOrderId = "gw_order_123",
            Status = PaymentIntentStatus.PAID.ToString(), // already processed
            ItemsSnapshot = "[]",
            Order = existingOrder
        };
        _paymentIntentRepo.Setup(r => r.GetByRazorpayOrderIdAsync("gw_order_123")).ReturnsAsync(paymentIntent);

        var request = new PaymentConfirmRequest("gw_order_123", "gw_pay_456", "any_signature");
        var result = await _sut.ConfirmPaymentAsync(doctorId: 1, doctorName: "Dr. Test", request);

        Assert.That(result.Status, Is.EqualTo(PaymentIntentStatus.PAID));
        Assert.That(result.OrderId, Is.EqualTo(99));
        // Must NOT create a second order on a retried confirm.
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Test]
    public async Task ConfirmPaymentAsync_Should_Create_Order_When_Valid_And_Not_Yet_Processed()
    {
        _paymentGatewayClient
            .Setup(g => g.VerifySignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var paymentIntent = new PaymentIntent
        {
            Id = 5,
            DoctorId = 1,
            RazorpayOrderId = "gw_order_123",
            Status = PaymentIntentStatus.CREATED.ToString(),
            Amount = 20m,
            ItemsSnapshot = "[{\"DrugId\":1,\"DrugName\":\"Paracetamol\",\"Quantity\":2,\"UnitPrice\":10}]"
        };
        _paymentIntentRepo.Setup(r => r.GetByRazorpayOrderIdAsync("gw_order_123")).ReturnsAsync(paymentIntent);

        var request = new PaymentConfirmRequest("gw_order_123", "gw_pay_456", "valid_signature");
        var result = await _sut.ConfirmPaymentAsync(doctorId: 1, doctorName: "Dr. Test", request);

        Assert.That(result.Status, Is.EqualTo(PaymentIntentStatus.PAID));
        _orderRepo.Verify(r => r.AddAsync(It.Is<Order>(o => o.DoctorId == 1 && o.TotalAmount == 20m)), Times.Once);
        _orderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}

