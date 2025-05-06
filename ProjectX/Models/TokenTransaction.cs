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

    public int AmountToken { get; set; }
    [Column(TypeName = "nvarchar(50)")] public TokenTransactionType Type { get; set; }
    public Guid? JobId { get; set; }

    [JsonIgnore] [ForeignKey("JobId")] public Job? Job { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum TokenTransactionType
{
    TopUp,
    ViewHiddenJobDetails,
    PurchaseJobService
}