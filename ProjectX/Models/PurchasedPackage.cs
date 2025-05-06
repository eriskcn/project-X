using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class PurchasedPackage : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessPackageId { get; set; }

    [JsonIgnore]
    [ForeignKey("BusinessPackageId")]
    public BusinessPackage BusinessPackage { get; set; } = null!;

    public Guid UserId { get; set; }
    [JsonIgnore] [ForeignKey("UserId")] public User User { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime NextResetDate { get; set; }
    public DateTime EndDate { set; get; }
    public int XTokenBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}