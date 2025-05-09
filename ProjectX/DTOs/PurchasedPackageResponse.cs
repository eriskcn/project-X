namespace ProjectX.DTOs;

public class PurchasedPackageResponse
{
    public Guid Id { set; get; }
    public BusinessPackageResponse BusinessPackage { set; get; } = null!;
    public bool IsActive { set; get; }
    public DateTime StartDate { set; get; }
    public DateTime EndDate { set; get; }
    public DateTime Created { set; get; }
    public DateTime Modified { set; get; }
}