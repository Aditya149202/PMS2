using SupplierInventoryService.Entities;

namespace SupplierInventoryService.Repositories.Interfaces;

public interface IStockReservationRepository
{
    // Tracked entities — caller mutates Status directly and saves via the shared DbContext.
    // Returns all ACTIVE reservations for this PaymentIntent (one row per drug line item).
    Task<List<StockReservation>> GetActiveByPaymentIntentIdAsync(int paymentIntentId);

    Task AddAsync(StockReservation reservation);
    Task SaveChangesAsync();
}