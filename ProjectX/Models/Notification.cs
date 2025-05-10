using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjectX.Data;

namespace ProjectX.Models;

public class Notification : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationType Type { get; set; }

    public Guid RecipientId { get; set; }

    [JsonIgnore]
    [ForeignKey("RecipientId")]
    public User Recipient { get; set; } = null!;

    public Guid TargetId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? Read { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    NewReactToPost, // post id 
    NewApplication, // campaign id
    NewComment, // post id
    NewAppointment, // appointment id
    UpcomingAppointment, // appointment id
    SuccessPayment, // payment id
    ApplicationSeen, // application id
    BusinessPackageExpired, // purchased packed id
    ApplicationRejected, // application id
    AcceptRecruiter, // user id
    RejectRecruiter, // user id
    AcceptJob,  // campaign id
    RejectJob, //campaign id
}