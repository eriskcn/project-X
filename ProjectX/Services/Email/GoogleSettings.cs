namespace ProjectX.Services.Email;

public class GoogleSettings
{
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string MailRefreshToken { get; set; }
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
}