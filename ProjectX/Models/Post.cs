using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ProjectX.Data;

namespace ProjectX.Models;

public class Post : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [StringLength(1000)] public required string Content { get; set; }
    [Range(0, int.MaxValue)] public int Point { get; set; }

    public bool IsEdited { get; set; }
    public DateTime? Edited { get; set; }

    public Guid UserId { get; set; }

    [JsonIgnore]
    [ForeignKey("UserId")]
    [InverseProperty("Posts")]
    public User User { get; set; } = null!;

    [JsonIgnore]
    [InverseProperty("LikedPosts")]
    public ICollection<User> LikedUsers { get; set; } = new List<User>();

    [JsonIgnore]
    [InverseProperty("DislikedPosts")]
    public ICollection<User> DislikedUsers { get; set; } = new List<User>();

    public Guid? ParentId { get; set; }
    [JsonIgnore] [ForeignKey("ParentId")] public Post ParentPost { get; set; } = null!;

    [JsonIgnore] public ICollection<Post> ChildrenPosts { get; set; } = new List<Post>();

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}