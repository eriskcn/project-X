using ProjectX.Models;

namespace ProjectX.DTOs;

public class JobServiceResponse
{
    public Guid Id { set; get; }
    public ServiceType Type { set; get; }
    public bool IsActive { set; get; }
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}