using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;
using ProjectX.Services.Notifications;

namespace ProjectX.Services;

public class BusinessPackageService(IServiceProvider serviceProvider, ILogger<BusinessPackageService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var nowUtc7 = nowUtc.AddHours(7);

                var nextRunUtc7 = new DateTime(
                    nowUtc7.Year,
                    nowUtc7.Month,
                    nowUtc7.Day,
                    1, 0, 0);

                if (nowUtc7 > nextRunUtc7)
                {
                    nextRunUtc7 = nextRunUtc7.AddDays(1);
                }

                var delay = nextRunUtc7 - nowUtc7;
                logger.LogInformation("BusinessPackageService sleeping for {Delay} until next 1AM UTC+7.", delay);
                await Task.Delay(delay, stoppingToken);

                logger.LogInformation("BusinessPackageService started at {Time}.", DateTime.UtcNow.AddHours(7));

                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var todayDateOnly = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

                var packages = dbContext.PurchasedPackages
                    .Include(p => p.BusinessPackage)
                    .Include(p => p.User)
                    .Where(p => p.IsActive)
                    .ToList();

                var totalScanned = packages.Count;
                var expiredCount = 0;
                var rewardedCount = 0;

                foreach (var pkg in packages)
                {
                    var isExpired = pkg.EndDate <= DateTime.UtcNow;
                    var resetDateMatch = DateOnly.FromDateTime(pkg.NextResetDate) == todayDateOnly;

                    if (isExpired)
                    {
                        pkg.IsActive = false;
                        pkg.User.Level = AccountLevel.Standard;
                        expiredCount++;

                        await notificationService.SendNotificationAsync(
                            NotificationType.BusinessPackageExpired,
                            pkg.User.Id,
                            pkg.Id
                        );

                        logger.LogInformation("Expired package: UserId={UserId}, PackageId={PackageId}", pkg.User.Id,
                            pkg.Id);
                    }
                    else if (resetDateMatch)
                    {
                        pkg.User.XTokenBalance += pkg.BusinessPackage.MonthlyXTokenRewards;
                        pkg.NextResetDate = pkg.NextResetDate.AddMonths(1);
                        rewardedCount++;

                        logger.LogInformation("Monthly token reward granted: UserId={UserId}, Tokens={Tokens}",
                            pkg.User.Id, pkg.BusinessPackage.MonthlyXTokenRewards);
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "BusinessPackageService completed. Scanned: {Scanned}, Expired: {Expired}, Rewarded: {Rewarded}",
                    totalScanned, expiredCount, rewardedCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BusinessPackageService encountered an error: {Message}", ex.Message);
            }
        }
    }
}