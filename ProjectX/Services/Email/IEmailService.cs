namespace ProjectX.Services.Email;

public interface IEmailService
{
    Task SendOtpViaEmailAsync(string email);
}