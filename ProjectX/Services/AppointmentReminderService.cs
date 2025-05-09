using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Services.Email;
using ProjectX.Services.Notifications;
using ProjectX.Models;

namespace ProjectX.Services;

public class AppointmentReminderService(
    ILogger<AppointmentReminderService> logger,
    IServiceProvider serviceProvider
)
    : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Appointment Reminder Service starting.");

        // Calculate time until next 3 AM UTC+7
        var utcNow = DateTime.UtcNow;
        var utcPlus7 =
            TimeZoneInfo.ConvertTimeFromUtc(utcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); // UTC+7

        var nextRun = new DateTime(
            utcPlus7.Year,
            utcPlus7.Month,
            utcPlus7.Day,
            3, 0, 0,
            DateTimeKind.Unspecified);

        if (utcPlus7 >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }

        var timeUntilNextRun = nextRun - utcPlus7;

        // Convert back to UTC for the timer
        var utcNextRun =
            TimeZoneInfo.ConvertTimeToUtc(nextRun, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        var utcTimeUntilNextRun = utcNextRun - utcNow;

        _timer = new Timer(DoWork, null, utcTimeUntilNextRun, TimeSpan.FromDays(1));
        // _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            logger.LogInformation("Running appointment reminder check at {Time}", DateTime.UtcNow);

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            
            var now = DateTime.UtcNow;
            var endOfWindow = now.AddHours(24);

            var appointments = await dbContext.Appointments
                .Include(a => a.Application)
                .ThenInclude(app => app.Candidate)
                .Include(a => a.Application)
                .ThenInclude(app => app.Job)
                .ThenInclude(j => j.Campaign)
                .ThenInclude(c => c.Recruiter)
                .Where(a => a.StartTime >= now && a.StartTime <= endOfWindow)
                .ToListAsync();

            var emailsSent = 0;

            foreach (var appointment in appointments)
            {
                await emailService.SendAppointmentReminderViaEmailAsync(
                    appointment.Application.Job.Campaign.Recruiter.Email!,
                    appointment);
                emailsSent++;
                await emailService.SendAppointmentReminderViaEmailAsync(
                    appointment.Application.Candidate.Email!,
                    appointment);
                emailsSent++;

                await notificationService.SendNotificationAsync(
                    NotificationType.UpcomingAppointment,
                    appointment.Application.CandidateId,
                    appointment.Id);
                await notificationService.SendNotificationAsync(
                    NotificationType.UpcomingAppointment,
                    appointment.Application.Job.Campaign.RecruiterId,
                    appointment.Id);
            }

            logger.LogInformation("Completed sending reminders. Success: {EmailsSent}",
                emailsSent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing appointment reminders.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Appointment Reminder Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}