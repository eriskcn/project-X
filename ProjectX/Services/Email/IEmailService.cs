namespace ProjectX.Services;

public interface IEmailService
{
    Task SendOtpViaEmailAsync(string email);
}