using ProjectX.DTOs.Orders;
using ProjectX.Models;

namespace ProjectX.DTOs.Stats;

public class AdminStats
{
    public AdminMonthStats AdminMonthStats { get; set; } = null!;
    public ICollection<DailyJobCount> DailyJobCounts { get; set; } = new List<DailyJobCount>();
    public ICollection<DailyRevenue> DailyRevenues { get; set; } = new List<DailyRevenue>();
    public RevenueByType RevenueByType { get; set; } = null!;
    public ICollection<OrderShortResponse> FiveLatestOrder { get; set; } = new List<OrderShortResponse>();
}

public class AdminMonthStats
{
    public Revenue Revenue { get; set; } = null!;
    public JobCount JobCount { get; set; } = null!;
    public CompanyCount CompanyCount { get; set; } = null!;
    public FreelancerCount FreelancerCount { get; set; } = null!;
    public CandidateCount CandidateCount { get; set; } = null!;
}

public class Revenue
{
    public double Total { get; set; }
    public double RateCompared { get; set; }
}

public class JobCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

public class CompanyCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

public class FreelancerCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

public class CandidateCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

public class DailyJobCount
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class DailyRevenue
{
    public DateOnly Date { get; set; }
    public double Revenue { get; set; }
}

public class RevenueByType
{
    public double TopUpRevenue { get; set; }
    public int TopUpCount { get; set; }
    public double JobServiceRevenue { get; set; }
    public int JobServiceCount { get; set; }
    public double BusinessPackageRevenue { get; set; }
    public int BusinessPackageCount { get; set; }
}

public class OrderShortResponse
{
    public Guid Id { get; set; }
    public UserResponse User { get; set; } = null!;
    public double Amount { get; set; }
    public OrderType Type { get; set; }
    public PaymentGateway Gateway { set; get; }
    public OrderStatus Status { get; set; }
    public DateTime Created { get; set; }
}