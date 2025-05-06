using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Payment : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid OrderId { get; set; }

    [ForeignKey("OrderId")] [JsonIgnore] public Order Order { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string Gateway { get; set; } = string.Empty;

    // Trường cho VNPay
    [StringLength(50)] public string VnpTxnRef { get; set; } = string.Empty;

    public double VnpAmount { get; set; }

    [StringLength(2)] public string VnpResponseCode { get; set; } = string.Empty;

    [StringLength(2)] public string? VnpTransactionStatus { get; set; } = string.Empty;

    [StringLength(256)] public string VnpSecureHash { get; set; } = string.Empty;

    public DateTime? VnpPayDate { get; set; }

    // Trường cho SePay
    [StringLength(255)] public string TransactionRef { get; set; } = string.Empty; // reference_number

    [Column(TypeName = "decimal(20,2)")] public decimal AmountIn { get; set; } // amount_in

    [Column(TypeName = "decimal(20,2)")] public decimal AmountOut { get; set; } // amount_out

    [Column(TypeName = "decimal(20,2)")] public decimal Accumulated { get; set; } // accumulated

    [StringLength(100)] public string AccountNumber { get; set; } = string.Empty; // account_number

    [StringLength(250)] public string SubAccount { get; set; } = string.Empty; // sub_account

    [StringLength(250)] public string? Code { get; set; }

    [StringLength(500)] public string TransactionContent { get; set; } = string.Empty; // transaction_content

    [StringLength(1000)] public string Body { get; set; } = string.Empty; // body

    [StringLength(50)] public string TransferType { get; set; } = string.Empty; // Thêm TransferType

    public long? WebhookTransactionId { get; set; } // Thêm để lưu Id từ webhook

    public DateTime? TransactionDate { get; set; } // transaction_date

    [Column(TypeName = "nvarchar(50)")] public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "nvarchar(50)")] public PaymentGateway PaymentGateway { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Expired,
    Completed,
    Failed,
    Refunded
}

public enum PaymentGateway
{
    VnPay,
    SePay
}