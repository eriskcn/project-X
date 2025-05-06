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

    public Guid UserId { get; set; }
    [JsonIgnore] public User User { get; set; } = null!;

    public Guid TargetId { get; set; }
    [Column(TypeName = "nvarchar(50)")] public OrderType Type { get; set; }
    public double Amount { get; set; }
    public PaymentGateway Gateway { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
    [JsonIgnore] public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum OrderType
{
    Business, // TargetId is PurchasedPackage.Id ??
    Job, // TargetId is Job.Id 
    TopUp // TargetId is TokenTransaction.Id
}