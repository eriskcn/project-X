using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class BusinessPackage : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(256)] public string Name { get; set; } = null!;
    [StringLength(256)] public string Description { get; set; } = null!;
    public BusinessLevel Level { get; set; }
    public double CashPrice { get; set; }
    public int DurationInDays { get; set; }
    public int MonthlyXTokenRewards { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    [JsonIgnore] public ICollection<PurchasedPackage> PurchasedPackages { get; set; } = new List<PurchasedPackage>();
}

public enum BusinessLevel
{
    Premium,
    Elite
}