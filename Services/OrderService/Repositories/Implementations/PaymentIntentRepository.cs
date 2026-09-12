using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;
using OrderService.Repositories.Interfaces;

namespace OrderService.Repositories.Implementations;

public class PaymentIntentRepository : IPaymentIntentRepository
{
    private readonly OrdersDbContext _context;

    public PaymentIntentRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentIntent paymentIntent)
    {
        await _context.PaymentIntents.AddAsync(paymentIntent);
    }

    public async Task<PaymentIntent?> GetByIdAsync(int id)
    {
        return await _context.PaymentIntents
        .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PaymentIntent?> GetByRazorpayOrderIdAsync(string razorpayOrderId)
    {
        return await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.RazorpayOrderId == razorpayOrderId);
    }
    public async Task<List<PaymentIntent>> GetExpiredPaymentIntentsAsync(DateTime cutoff)
    {
        return await _context.PaymentIntents
            .Where(p => p.Status == Enums.PaymentIntentStatus.CREATED.ToString() && p.CreatedAt < cutoff)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}