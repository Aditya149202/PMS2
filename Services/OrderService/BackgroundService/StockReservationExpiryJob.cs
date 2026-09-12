using BackgroundServices=Microsoft.Extensions.Hosting.BackgroundService;
using OrderService.Enums;
using OrderService.Repositories.Interfaces;
using OrderService.Services;
using Microsoft.Extensions.DependencyInjection;
namespace OrderService.BackgroundService;

public class StockReservationExpiryJob : BackgroundServices
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReservationExpiryJob> _logger;

    private readonly TimeSpan _interval;
    private readonly TimeSpan _paymentWindow;
    public StockReservationExpiryJob(IServiceScopeFactory scopeFactory, ILogger<StockReservationExpiryJob> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue<double?>("StockReservationExpiryJob:IntervalMinutes") ?? 15);
        _paymentWindow = TimeSpan.FromMinutes(config.GetValue<double?>("StockReservationExpiryJob:PaymentWindowMinutes") ?? 15);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {   
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            

            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reservation expiry job failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var paymentIntentRepository = scope.ServiceProvider.GetRequiredService<IPaymentIntentRepository>();

        var expiredPayments = await paymentIntentRepository.GetExpiredPaymentIntentsAsync(DateTime.UtcNow - _paymentWindow);

        foreach (var payment in expiredPayments)
        {
            try
            {
                await paymentService.ExpiredPaymentIntentAsync(payment.Id);
                _logger.LogInformation("Expired payment intent {PaymentIntentId}.", payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired payment intent {PaymentIntentId}.", payment.Id);
            }
        }
    }
}