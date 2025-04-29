using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class TokenTransaction : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid UserId { get; set; }

    [ForeignKey("UserId")] [JsonIgnore] public User User { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public TokenTransactionType Type { get; set; }

    [Required] [Range(0, double.MaxValue)] public double Amount { get; set; }

    [Column(TypeName = "nvarchar(50)")] public TokenTransactionPurpose Purpose { get; set; }

    public Guid? OrderId { get; set; }

    [ForeignKey("OrderId")] [JsonIgnore] public Order? Order { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum TokenTransactionType
{
    TopUp,
    Spend
}

public enum TokenTransactionPurpose
{
    HighlightJob,
    ViewHiddenStats,
    TopUp,
}