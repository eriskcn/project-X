using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Services.QR;

public class VietQrService(ApplicationDbContext context) : IVietQrService
{
    private const string Bank = "bidv";
    private const string BankAccount = "96247020720003";
    private const string AccountName = "ProjectX";
    private const string QuickLinkUrl = "https://img.vietqr.io/image/";

    public string GenerateQuickLink(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var quickLink =
            $"{QuickLinkUrl}{Bank}-{BankAccount}-compact.png?amount={order.Amount}&addInfo=PAY{order.Id:N}&accountName={AccountName}";
        return quickLink;
    }

    public async Task<string> GenerateQuickLink(Payment payment, CancellationToken cancellationToken = default)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(payment, nameof(payment));

        // Retrieve order from database
        var order = await context.Orders.FindAsync(new object[] { payment.OrderId }, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with ID {payment.OrderId} not found.");
        }

        // Validate order amount
        if (order.Amount <= 0)
        {
            throw new InvalidOperationException("Order amount must be greater than zero.");
        }

        // Validate configuration values
        if (string.IsNullOrWhiteSpace(QuickLinkUrl) || string.IsNullOrWhiteSpace(Bank) ||
            string.IsNullOrWhiteSpace(BankAccount) || string.IsNullOrWhiteSpace(AccountName))
        {
            throw new InvalidOperationException("Quick link configuration values are missing or invalid.");
        }

        // Ensure payment.Id is a valid GUID
        if (payment.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Payment ID is invalid.");
        }

        // Generate quick link
        var quickLink = $"{QuickLinkUrl}{Uri.EscapeDataString(Bank)}-{Uri.EscapeDataString(BankAccount)}-compact.png" +
                        $"?amount={order.Amount:F2}&addInfo=PAY{payment.Id:N}&accountName={Uri.EscapeDataString(AccountName)}";

        return quickLink;
    }
}