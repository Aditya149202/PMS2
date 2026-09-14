using Moq;
using OrderService.Clients;
using OrderService.Entities;
using OrderService.Enums;
using OrderService.ExceptionMiddleware;
using OrderService.Repositories.Interfaces;
using OrderServiceUnderTest = OrderService.Services.OrderService;

namespace OrderService.Tests.Services;

[TestFixture]
public class OrderServiceTests
{
    private Mock<IOrderRepository> _orderRepo;
    private Mock<ISupplierInventoryClient> _supplierInventoryClient;
    private OrderServiceUnderTest _sut;

    [SetUp]
    public void Setup()
    {
        _orderRepo = new Mock<IOrderRepository>();
        _supplierInventoryClient = new Mock<ISupplierInventoryClient>();
        _sut = new OrderServiceUnderTest(_orderRepo.Object, _supplierInventoryClient.Object);
    }

    private static Order MakeOrder(int id, OrderStatus status, int doctorId = 1) => new()
    {
        Id = id,
        DoctorId = doctorId,
        DoctorNameSnapshot = "Dr. Test",
        Status = status.ToString(),
        TotalAmount = 100m,
        PaymentIntentId = 50,
        CreatedAt = DateTime.UtcNow,
        OrderItems = new List<OrderItem>()
    };

    [Test]
    public async Task VerifyOrderAsync_Should_Verify_When_Status_Is_New()
    {
        var order = MakeOrder(1, OrderStatus.NEW);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _sut.VerifyOrderAsync(1);

        Assert.That(result.Status, Is.EqualTo(OrderStatus.VERIFIED));
        Assert.That(order.VerifiedAt, Is.Not.Null);
        _orderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void VerifyOrderAsync_Should_Throw_InvalidOrderStateException_When_Not_New()
    {
        var order = MakeOrder(1, OrderStatus.VERIFIED);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        Assert.ThrowsAsync<InvalidOrderStateException>(() => _sut.VerifyOrderAsync(1));
    }

    // This is the exact regression test for the bug you fixed — before the fix, this
    // threw AppValidationException (400) instead of InvalidOrderStateException (409).
    [Test]
    public void PickupOrderAsync_On_NotVerified_Order_Should_Throw_InvalidOrderStateException_Not_AppValidationException()
    {
        var order = MakeOrder(1, OrderStatus.NEW);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        Assert.ThrowsAsync<InvalidOrderStateException>(() => _sut.PickupOrderAsync(1));
        _supplierInventoryClient.Verify(
            c => c.CommitSaleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task PickupOrderAsync_Should_Complete_And_Call_CommitSale_When_Verified()
    {
        var order = MakeOrder(1, OrderStatus.VERIFIED);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _sut.PickupOrderAsync(1);

        Assert.That(result.Status, Is.EqualTo(OrderStatus.COMPLETED));
        _supplierInventoryClient.Verify(c => c.CommitSaleAsync(1, order.PaymentIntentId, order.TotalAmount), Times.Once);
        _orderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task PickupOrderAsync_When_CommitSaleAsync_Throws_Should_Not_Change_Order_Status()
    {
        // Confirms the "stays VERIFIED, safe to retry" guarantee described in the code comment.
        var order = MakeOrder(1, OrderStatus.VERIFIED);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _supplierInventoryClient
            .Setup(c => c.CommitSaleAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .ThrowsAsync(new HttpRequestException("downstream failure"));

        Assert.ThrowsAsync<HttpRequestException>(() => _sut.PickupOrderAsync(1));
        Assert.That(order.Status, Is.EqualTo(OrderStatus.VERIFIED.ToString()));
        _orderRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        await Task.CompletedTask;
    }

    [Test]
    public void CancelOrderAsync_Doctor_Should_Throw_When_Order_Already_Verified()
    {
        // Doctors can only cancel NEW orders — VERIFIED is Admin-only to cancel.
        var order = MakeOrder(1, OrderStatus.VERIFIED, doctorId: 7);

        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        Assert.ThrowsAsync<InvalidOrderStateException>(
            () => _sut.CancelOrderAsync(1, CancelledBy.DOCTOR, requestingDoctorId: 7));
    }

    [Test]
    public async Task CancelOrderAsync_Admin_Should_Cancel_Verified_Order_And_Release_Reservation()
    {
        var order = MakeOrder(1, OrderStatus.VERIFIED);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(1, CancelledBy.ADMIN, requestingDoctorId: null);

        Assert.That(result.Status, Is.EqualTo(OrderStatus.CANCELLED));
        _supplierInventoryClient.Verify(c => c.ReleaseReservationAsync(order.PaymentIntentId), Times.Once);
    }

    [Test]
    public void GetOrderByIdAsync_Should_Throw_NotFound_When_Doctor_Does_Not_Own_Order()
    {
        // Anti-enumeration: a mismatched owner gets the SAME exception as a missing id.
        var order = MakeOrder(1, OrderStatus.NEW, doctorId: 7);
        _orderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);

        Assert.ThrowsAsync<NotFoundException>(() => _sut.GetOrderByIdAsync(1, requestingDoctorId: 99));
    }
}
