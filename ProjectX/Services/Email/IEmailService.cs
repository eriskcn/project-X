using ProjectX.Models;

namespace ProjectX.Services.Email;

public interface IEmailService
{
    Task SendOtpViaEmailAsync(string email);
    Task SendNewPasswordViaEmailAsync(string email);
    Task SendAppointmentReminderViaEmailAsync(string email, Appointment appointment);
}