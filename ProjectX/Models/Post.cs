using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Models;

public class Post
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required string Id { get; set; }

    [Required] [StringLength(1000)] public required string Content { get; set; }
    [Range(0, int.MaxValue)] public int Point { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsEdited { get; set; } = false;

    // Relationship
    [Required] [StringLength(450)] public required string UserId { get; set; }
    [ForeignKey("UserId")] public User User { get; set; } = null!;

    public ICollection<User> LikedUsers { get; set; } = new List<User>();

    public ICollection<User> DislikedUsers { get; set; } = new List<User>();

    [StringLength(450)] public string? ParentId { get; set; }
    [ForeignKey("ParentId")] public Post ParentPost { get; set; } = null!;

    public ICollection<Post> ChildrenPosts { get; set; } = new List<Post>();

    [StringLength(450)] public string? AttachedFileId { get; set; }
    [ForeignKey("AttachedFileId")] public File? AttachedFile { get; set; } = null!;

    // Tracking
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}