using Google.Apis.Auth.OAuth2;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using ProjectX.Data;

namespace ProjectX.Services.Email;

public class EmailService(IOptions<GoogleSettings> emailSettings, ApplicationDbContext context) : IEmailService
{
    private readonly GoogleSettings _googleSettings = emailSettings.Value;

    public async Task SendOtpViaEmailAsync(string email)
    {
        var receiver = await context.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (receiver == null)
        {
            throw new Exception("User not found");
        }

        var otp = GenerateOtp();
        receiver.OTP = otp;
        receiver.OTPExpiry = DateTime.UtcNow.AddMinutes(5);
        await context.SaveChangesAsync();

        var subject = "Xác thực đăng ký tài khoản mới tại ProjectX";
        var textBody =
            $"Mã OTP để xác thực tài khoản của bạn là: {otp}. Mã có hiệu lực trong vòng 5 phút, vui lòng không chia sẻ mã này với bất kỳ ai.";
        var htmlBody =
            $"<p>Mã OTP để xác thực tài khoản của bạn là: <strong>{otp}</strong>.</p><p>Mã có hiệu lực trong vòng 5 phút, vui lòng không chia sẻ mã này với bất kỳ ai.</p>";

        await SendEmailAsync(email, subject, textBody, htmlBody);
    }

    private async Task SendEmailAsync(string to, string subject, string text, string html)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_googleSettings.SenderName, _googleSettings.SenderEmail));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = text,
                HtmlBody = html
            };
            email.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_googleSettings.SmtpServer, _googleSettings.SmtpPort,
                SecureSocketOptions.StartTls);

            var oauth2 = new SaslMechanismOAuth2(_googleSettings.SenderEmail, accessToken);
            await client.AuthenticateAsync(oauth2);

            await client.SendAsync(email);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send email: {ex.Message}", ex);
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        try
        {
            // Debug: In ra thông tin cấu hình
            Console.WriteLine($"ClientID: {_googleSettings.ClientId}");
            Console.WriteLine($"ClientSecret: {_googleSettings.ClientSecret}");
            Console.WriteLine($"RefreshToken: {_googleSettings.MailRefreshToken}");

            var jsonPayload = $$"""
                                {
                                    "client_id": "{{_googleSettings.ClientId}}",
                                    "client_secret": "{{_googleSettings.ClientSecret}}",
                                    "refresh_token": "{{_googleSettings.MailRefreshToken}}",
                                    "type": "authorized_user"
                                }
                                """;

            Console.WriteLine($"JSON Payload: {jsonPayload}"); // Debug JSON

            var credential = GoogleCredential.FromJson(jsonPayload)
                .CreateScoped("https://mail.google.com/");

            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            Console.WriteLine($"AccessToken: {accessToken}"); // Debug AccessToken
            return accessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex}"); // Log toàn bộ exception
            throw new Exception("Failed to get OAuth2 access token. Details: " + ex.Message, ex);
        }
    }

    private static string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}