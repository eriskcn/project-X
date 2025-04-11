using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Like : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required] public required Guid UserId { get; set; }
    [JsonIgnore] [ForeignKey("UserId")] public User User { get; set; } = null!;
    [Required] public required Guid PostId { get; set; }
    [ForeignKey("PostId")] public Post Post { get; set; } = null!;
    public required bool IsLike { get; set; }
}