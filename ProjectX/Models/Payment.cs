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

    [ForeignKey("OrderId")] public Order Order { get; set; } = null!;

    [StringLength(50)] public string VnpTxnRef { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")] public decimal VnpAmount { get; set; }

    [StringLength(2)] public string VnpResponseCode { get; set; } = string.Empty;

    [StringLength(2)] public string VnpTransactionStatus { get; set; } = string.Empty;

    [StringLength(256)] public string VnpSecureHash { get; set; } = string.Empty;

    public DateTime? VnpPayDate { get; set; }

    [Column(TypeName = "nvarchar(50)")] public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}