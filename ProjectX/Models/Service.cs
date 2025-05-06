using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Service : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(150)] public required string Name { get; set; }
    [StringLength(500)] public required string Description { get; set; }
    public required int DayLimit { get; set; }
    public ServiceType Type { get; set; }
    public double CashPrice { get; set; }
    public int XTokenPrice { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    [JsonIgnore] public ICollection<JobService> JobServices { get; set; } = new List<JobService>();
}

public enum ServiceType
{
    Highlight,
    Urgent,
    Hot
}