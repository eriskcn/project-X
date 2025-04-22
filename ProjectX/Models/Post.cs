using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Post : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(1000)] public required string Content { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? Edited { get; set; }

    public Guid UserId { get; set; }

    [JsonIgnore]
    [ForeignKey("UserId")]
    [InverseProperty("Posts")]
    public User User { get; set; } = null!;

    public Guid? ParentId { get; set; }
    [JsonIgnore] [ForeignKey("ParentId")] public Post? ParentPost { get; set; }

    [JsonIgnore] public ICollection<Post> ChildrenPosts { get; set; } = new List<Post>();
    [JsonIgnore] public ICollection<Like> Likes { get; set; } = new List<Like>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}