using Microsoft.EntityFrameworkCore;
using SupplierInventoryService.Data;
using SupplierInventoryService.Entities;
using SupplierInventoryService.Enums;
using SupplierInventoryService.Repositories.Interfaces;

namespace SupplierInventoryService.Repositories.Implementations;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly SupplierInventoryDbContext _context;

    public StockReservationRepository(SupplierInventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockReservation>> GetActiveByPaymentIntentIdAsync(int paymentIntentId)
    {
        return await _context.StockReservations
            .Where(r => r.PaymentIntentId == paymentIntentId && r.Status == StockReservationStatus.ACTIVE.ToString())
            .ToListAsync();
    }
    public async Task AddAsync(StockReservation reservation)
    {
        await _context.StockReservations.AddAsync(reservation);
    }
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}

