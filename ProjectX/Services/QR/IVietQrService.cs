using ProjectX.Models;

namespace ProjectX.Services.QR;

public interface IVietQrService
{
    string GenerateQuickLink(Order order);
    Task<string> GenerateQuickLink(Payment payment, CancellationToken cancellationToken = default);
}