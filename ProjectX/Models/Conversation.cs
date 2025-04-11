using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Conversation : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(100)] public string? Name { get; set; }
    public bool IsStored { get; set; }

    public DateTime? Stored { get; set; }

    [JsonIgnore] public ICollection<Message> Messages { get; set; } = new List<Message>();
    [JsonIgnore] public ICollection<User> Participants { get; set; } = new List<User>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}