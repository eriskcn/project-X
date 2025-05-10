using System.Collections;
using ProjectX.Models;

namespace ProjectX.DTOs.Stats;

public class RecruitmentStats
{
    public RecruitmentMonthStats RecruitmentMonthStats { get; set; } = null!;
    public ICollection<DailyApplication> DailyApplications { get; set; } = new List<DailyApplication>();
    public ICollection<TopJob> TopJobs { get; set; } = new List<TopJob>();
    public ICollection<ShortApplication> FiveLatestApplications { get; set; } = new List<ShortApplication>();
    public int ApplicationCountInLatestWeek { get; set; }
}

public class RecruitmentMonthStats
{
    public ApplicationCount ApplicationCount { get; set; } = null!;
    public JobCount JobCount { get; set; } = null;
    public SeenApplicationCount SeenApplicationCount { get; set; } = null!;
    public HiredRate HiredRate { get; set; } = null!;
}

public class ApplicationCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

// JobCount co roi

public class SeenApplicationCount
{
    public int Count { get; set; }
    public double RateCompared { get; set; }
}

public class HiredRate
{
    public double Rate { get; set; }
    public double RateCompared { get; set; }
}

public class DailyApplication
{
    public int TotalApplications { get; set; }
    public int SeenApplications { get; set; }
    public DateOnly Date { get; set; }
}

public class TopJob
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public int ApplicationCount { get; set; }
    public double Percentage { get; set; }
}

public class ShortApplication
{
    public Guid Id { get; set; }
    public UserResponse Candidate { get; set; } = null!;
    public required string JobTitle { get; set; }
    public Guid CampaignId { get; set; }
    public ApplicationProcess Process { get; set; }
    public DateTime? Submitted { get; set; }
}