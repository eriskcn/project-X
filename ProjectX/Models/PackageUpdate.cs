using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class PackageUpdate : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    [JsonIgnore] [ForeignKey("UserId")] public User User { get; set; } = null!;

    public Guid OriginalPackageId { get; set; }

    [JsonIgnore]
    [ForeignKey("OriginalPackageId")]
    public BusinessPackage OriginalPackage { get; set; } = null!;

    public Guid UpdatedPackageId { get; set; }

    [JsonIgnore]
    [ForeignKey("UpdatedPackageId")]
    public BusinessPackage UpdatedPackage { get; set; } = null!;

    public double PriceDifference { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}