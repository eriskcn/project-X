using Google.Apis.Auth.OAuth2;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using ProjectX.Data;
using ProjectX.Helpers;
using ProjectX.Models;

namespace ProjectX.Services.Email;

public class EmailService(
    IOptions<GoogleSettings> emailSettings,
    ApplicationDbContext context,
    UserManager<User> userManager) : IEmailService
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
            $"Xin chào {receiver.FullName},\n\n" +
            $"Mã OTP để xác thực tài khoản của bạn là: {otp}. Mã có hiệu lực trong vòng 5 phút, vui lòng không chia sẻ mã này với bất kỳ ai.\n\n" +
            "Trân trọng,\nProjectX Team";

        var htmlBody = $@"
        <div style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
            <h2 style='color: #2c3e50;'>Xin chào {receiver.FullName},</h2>
            <p>Mã OTP để xác thực tài khoản của bạn là:</p>
            <p style='font-size: 20px; font-weight: bold; color: #e74c3c;'>{otp}</p>
            <p>Mã có hiệu lực trong vòng <strong>5 phút</strong>.</p>
            <p style='color: #888;'>Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
            <br/>
            <p>Trân trọng,</p>
            <p><strong>ProjectX Team</strong></p>
        </div>";

        await SendEmailAsync(email, subject, textBody, htmlBody);
    }


    public async Task SendNewPasswordViaEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            throw new Exception("Không tìm thấy người dùng.");

        var newPassword = PasswordHelper.GenerateCompliantPassword();
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new Exception($"Đặt lại mật khẩu thất bại: {errors}");
        }

        var fullName = user.FullName ?? "Người dùng";
        var subject = "Mật khẩu mới của bạn từ ProjectX";

        var plainText = $@"
Xin chào {fullName},

Hệ thống đã thiết lập lại mật khẩu cho tài khoản của bạn.

Mật khẩu tạm thời mới của bạn là: {newPassword}

Vui lòng đăng nhập và thay đổi mật khẩu ngay để đảm bảo an toàn cho tài khoản.

Nếu bạn không yêu cầu đặt lại mật khẩu, hãy đăng nhập và đổi mật khẩu ngay lập tức để tránh mất quyền kiểm soát.

Trân trọng,
ProjectX Team
";

        var htmlContent = $@"
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            background-color: #f9f9f9;
        }}
        .header {{
            font-size: 20px;
            font-weight: bold;
            color: #2c3e50;
            margin-bottom: 10px;
        }}
        .warning {{
            color: #c0392b;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 20px;
            font-size: 13px;
            color: #999;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>Thông báo đặt lại mật khẩu</div>
        <p>Xin chào {fullName},</p>
        <p>Hệ thống đã thiết lập lại mật khẩu cho tài khoản của bạn.</p>
        <p>Mật khẩu tạm thời mới của bạn là: <strong>{newPassword}</strong></p>
        <p>
            <strong>Lưu ý:</strong> Vui lòng đăng nhập bằng mật khẩu này <span class='warning'>ngay khi có thể</span> và thay đổi mật khẩu để bảo vệ tài khoản của bạn.
        </p>
        <p class='warning'>
            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng đăng nhập ngay và đổi mật khẩu để đảm bảo an toàn cho tài khoản của mình.
        </p>
        <p>Trân trọng,<br/>Project X Team</p>
        <div class='footer'>
            Đây là email tự động, vui lòng không trả lời thư này.
        </div>
    </div>
</body>
</html>
";

        await SendEmailAsync(email, subject, plainText, htmlContent);
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