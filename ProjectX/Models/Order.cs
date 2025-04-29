using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Order : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid JobId { get; set; }

    [ForeignKey("JobId")] public Job Job { get; set; } = null!;

    [Required] public Guid UserId { get; set; }

    [ForeignKey("UserId")] [JsonIgnore] public User User { get; set; } = null!;

    [Range(1, 30)] public int Days { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }

    [Column(TypeName = "nvarchar(50)")] public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "nvarchar(50)")] public OrderType Type { get; set; } = OrderType.HighlightJob;

    [Column(TypeName = "nvarchar(50)")] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum OrderType
{
    HighlightJob,
    TopUpToken
}

public enum PaymentMethod
{
    Cash, 
    Token 
}

public enum OrderStatus
{
    Pending,
    Expired,
    Completed,
    Failed,
    Refunded
}