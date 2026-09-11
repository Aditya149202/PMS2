using SupplierInventoryService.Entities;
using SupplierInventoryService.Enums;
using SupplierInventoryService.ExceptionMiddleware;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Services;

public class InternalService : IInternalService
{
    private readonly IStockReservationRepository _reservationRepository;
    private readonly ISalesRepository _salesRepository;
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IDrugRepository _drugRepository;

    public InternalService(
        IStockReservationRepository reservationRepository,
        ISalesRepository salesRepository,
        IProcessedEventRepository processedEventRepository,
        IDrugRepository drugRepository)
    {
        _reservationRepository = reservationRepository;
        _salesRepository = salesRepository;
        _processedEventRepository = processedEventRepository;
        _drugRepository = drugRepository;
    }

    // Idempotent by design: if this orderId is already in ProcessedEvents, it's a retried
    // pickup call (e.g. OrderService's own SaveChangesAsync failed after this succeeded once
    // before) — return success without re-committing reservations or inserting a duplicate Sale.
    public async Task CommitSaleAsync(int orderId, int paymentIntentId, decimal amount)
    {
        if (await _processedEventRepository.ExistsAsync(orderId))
            return; // already processed — no-op, not an error

        var activeReservations = await _reservationRepository.GetActiveByPaymentIntentIdAsync(paymentIntentId);
        if (activeReservations.Count == 0)
            throw new NotFoundException($"No ACTIVE StockReservation found for PaymentIntent {paymentIntentId}.");

        foreach (var reservation in activeReservations)
            reservation.Status = StockReservationStatus.COMMITTED.ToString();

        await _salesRepository.AddAsync(new Sale
        {
            OrderId = orderId,
            Amount = amount,
            SaleDate = DateTime.UtcNow
        });

        await _processedEventRepository.AddAsync(new ProcessedEvent
        {
            OrderId = orderId,
            ProcessedAt = DateTime.UtcNow
        });

        // Single save across all three repos' pending changes (same DbContext instance).
        await _processedEventRepository.SaveChangesAsync();
    }

    // 404 here is deliberate, not swallowed — OrderService's ReleaseReservationAsync already
    // treats 404 as a no-op on its side, so "nothing ACTIVE to release" (already released,
    // or never existed) is safe to surface honestly rather than pretending to succeed here too.
    public async Task ReleaseReservationAsync(int paymentIntentId)
    {
        var activeReservations = await _reservationRepository.GetActiveByPaymentIntentIdAsync(paymentIntentId);
        if (activeReservations.Count == 0)
            throw new NotFoundException($"No ACTIVE StockReservation found for PaymentIntent {paymentIntentId}.");

        foreach (var reservation in activeReservations)
        {
            reservation.Status = StockReservationStatus.RELEASED.ToString();

            var drug = await _drugRepository.GetByIdAsync(reservation.DrugId)
                ?? throw new NotFoundException($"Drug {reservation.DrugId} referenced by reservation not found.");
            drug.QuantityInStock += reservation.Quantity;
        }

        await _reservationRepository.SaveChangesAsync();
    }

        public async Task<List<(int DrugId,string DrugName,decimal UnitPrice)>> ReserveStockAsync(
        int paymentIntentId, List<(int DrugId, int Quantity)> items)
    {
        var drugs = new List<(Drug Drug, int Quantity)>();

        foreach (var (drugId, quantity) in items)
        {
            var drug = await _drugRepository.GetByIdAsync(drugId)
                ?? throw new NotFoundException($"Drug {drugId} not found.");

            if (!drug.IsActive)
                throw new AppValidationException("drugId", $"Drug {drugId} is no longer available.");

            if (drug.QuantityInStock < quantity)
                throw new InsufficientStockException($"Insufficient stock for drug {drugId}.");

            drugs.Add((drug, quantity));
        }

        var results = new List<(int, string, decimal)>();
        foreach (var (drug, quantity) in drugs)
        {
            drug.QuantityInStock -= quantity;

            await _reservationRepository.AddAsync(new StockReservation
            {
                PaymentIntentId = paymentIntentId,
                DrugId = drug.Id,
                Quantity = quantity,
                Status = StockReservationStatus.ACTIVE.ToString(),
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15) // placeholder, same as the never-locked stale-order threshold
            });

            results.Add((drug.Id, drug.Name, drug.Price));
        }

        await _reservationRepository.SaveChangesAsync();
        return results;
    }
}