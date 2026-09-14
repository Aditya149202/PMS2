using Moq;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Enums;
using SupplierInventoryService.ExceptionMiddleware;
using SupplierInventoryService.Repositories.Interfaces;
using SupplierInventoryService.Services;

namespace SupplierInventoryService.Tests.Services;

[TestFixture]
public class InternalServiceTests
{
    private Mock<IStockReservationRepository> _reservationRepo;
    private Mock<ISalesRepository> _salesRepo;
    private Mock<IProcessedEventRepository> _processedEventRepo;
    private Mock<IDrugRepository> _drugRepo;
    private InternalService _sut;

    [SetUp]
    public void Setup()
    {
        _reservationRepo = new Mock<IStockReservationRepository>();
        _salesRepo = new Mock<ISalesRepository>();
        _processedEventRepo = new Mock<IProcessedEventRepository>();
        _drugRepo = new Mock<IDrugRepository>();

        _sut = new InternalService(
            _reservationRepo.Object, _salesRepo.Object, _processedEventRepo.Object, _drugRepo.Object);
    }

    // The core idempotency test: a retried commit for an already-processed order must be
    // a silent no-op — not a duplicate Sale, not a duplicate ProcessedEvent insert (which
    // is exactly what threw the identity-column error before the ValueGeneratedNever fix).
    [Test]
    public async Task CommitSaleAsync_Should_ReturnEarly_When_OrderId_Already_Processed()
    {
        _processedEventRepo.Setup(r => r.ExistsAsync(42)).ReturnsAsync(true);

        await _sut.CommitSaleAsync(orderId: 42, paymentIntentId: 5, amount: 100m);

        _reservationRepo.Verify(r => r.GetActiveByPaymentIntentIdAsync(It.IsAny<int>()), Times.Never);
        _salesRepo.Verify(r => r.AddAsync(It.IsAny<Sale>()), Times.Never);
        _processedEventRepo.Verify(r => r.AddAsync(It.IsAny<ProcessedEvent>()), Times.Never);
    }

    [Test]
    public void CommitSaleAsync_Should_Throw_NotFound_When_No_Active_Reservations()
    {
        _processedEventRepo.Setup(r => r.ExistsAsync(42)).ReturnsAsync(false);
        _reservationRepo.Setup(r => r.GetActiveByPaymentIntentIdAsync(5)).ReturnsAsync(new List<StockReservation>());

        Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CommitSaleAsync(orderId: 42, paymentIntentId: 5, amount: 100m));
    }

    [Test]
    public async Task CommitSaleAsync_Should_Commit_Reservations_Create_Sale_And_ProcessedEvent_When_Valid()
    {
        _processedEventRepo.Setup(r => r.ExistsAsync(42)).ReturnsAsync(false);

        var reservations = new List<StockReservation>
        {
            new() { Id = 1, PaymentIntentId = 5, DrugId = 10, Quantity = 2, Status = StockReservationStatus.ACTIVE.ToString() }
        };
        _reservationRepo.Setup(r => r.GetActiveByPaymentIntentIdAsync(5)).ReturnsAsync(reservations);

        await _sut.CommitSaleAsync(orderId: 42, paymentIntentId: 5, amount: 100m);

        Assert.That(reservations[0].Status, Is.EqualTo(StockReservationStatus.COMMITTED.ToString()));
        _salesRepo.Verify(r => r.AddAsync(It.Is<Sale>(s => s.OrderId == 42 && s.Amount == 100m)), Times.Once);
        _processedEventRepo.Verify(r => r.AddAsync(It.Is<ProcessedEvent>(pe => pe.OrderId == 42)), Times.Once);
        // Single shared-context save, per the code's own comment — not once per repo.
        _processedEventRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _reservationRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _salesRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}