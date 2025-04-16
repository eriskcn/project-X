using System.ComponentModel.DataAnnotations;

namespace ProjectX.DTOs;

public class RejectRequest
{
    [Required] [StringLength(500)] public required string RejectReason { get; set; }
}