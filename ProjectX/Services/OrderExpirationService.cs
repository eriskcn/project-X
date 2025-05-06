using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Services;

public class OrderExpirationService(IServiceProvider serviceProvider, ILogger<OrderExpirationService> logger)
    : BackgroundService
{
    private const int ExpireAfterMinutes = 15;
    private const int DelayMinutes = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderExpirationService is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;

                // Expire Orders
                // var expiredOrders = await context.Orders
                //     .Where(o => o.Status == OrderStatus.Pending && o.Created.AddMinutes(ExpireAfterMinutes) < now)
                //     .ToListAsync(stoppingToken);
                //
                // foreach (var order in expiredOrders)
                // {
                //     order.Status = OrderStatus.Expired;
                //     order.Modified = now;
                //     logger.LogInformation("Order {OrderId} expired.", order.Id);
                // }
                //
                // // Expire Payments
                // var expiredPayments = await context.Payments
                //     .Where(p => p.Status == PaymentStatus.Pending && p.Created.AddMinutes(ExpireAfterMinutes) < now)
                //     .ToListAsync(stoppingToken);
                //
                // foreach (var payment in expiredPayments)
                // {
                //     payment.Status = PaymentStatus.Expired;
                //     payment.Modified = now;
                //     logger.LogInformation("Payment {PaymentId} expired.", payment.Id);
                // }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while expiring orders/payments.");
            }

            await Task.Delay(TimeSpan.FromMinutes(DelayMinutes), stoppingToken);
        }

        logger.LogInformation("OrderExpirationService is stopping.");
    }
}