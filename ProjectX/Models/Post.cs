using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    // Relationship
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Posts")]
    public User User { get; set; } = null!;

    [InverseProperty("LikedPosts")] public ICollection<User> LikedUsers { get; set; } = new List<User>();
    [InverseProperty("DislikedPosts")] public ICollection<User> DislikedUsers { get; set; } = new List<User>();

    public Guid? ParentId { get; set; }
    [ForeignKey("ParentId")] public Post ParentPost { get; set; } = null!;

    public ICollection<Post> ChildrenPosts { get; set; } = new List<Post>();

    // public Guid? AttachedFileId { get; set; }
    // [ForeignKey("AttachedFileId")] public AttachedFile? AttachedFile { get; set; }
    public AttachedFile AttachedFile { get; set; } = null!;

    // Tracking
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
}