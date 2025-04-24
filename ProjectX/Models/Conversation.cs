using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectX.Models;

public class Conversation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public bool IsGroup { get; set; }
    public string? GroupName { get; set; }
    public string? GroupPicture { get; set; } = "/images/default-avatar.jpeg";
    public bool IsStored { get; set; }

    [JsonIgnore] public ICollection<User> Participants { get; set; } = new List<User>();
    [JsonIgnore] public ICollection<Message> Messages { get; set; } = new List<Message>();

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public DateTime LatestMessage { get; set; } = DateTime.UtcNow;
    public Guid? LatestMessageId { get; set; }
}