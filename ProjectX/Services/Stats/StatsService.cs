using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.DTOs.Stats;
using ProjectX.Models;

namespace ProjectX.Services.Stats;

public class StatsService(ApplicationDbContext context) : IStatsService
{
    public async Task<double> GetCompetitiveRateAsync(Job job)
    {
        var applications = await context.Applications
            .Where(x => x.JobId == job.Id && x.Status != ApplicationStatus.Draft)
            .ToListAsync();

        var viewCount = job.ViewCount;

        var savedCount = await context.Users
            .Where(u => u.SavedJobs.Any(j => j.Id == job.Id))
            .CountAsync();

        var applicationCount = applications.Count;
        var applicationShortListedCount = applications.Count(x => x.Process == ApplicationProcess.Shortlisted);
        var applicationInterviewing = applications.Count(x => x.Process == ApplicationProcess.Interviewing);
        var applicationOffered = applications.Count(x => x.Process == ApplicationProcess.Offered);
        var applicationHired = applications.Count(x => x.Process == ApplicationProcess.Hired);
        var quantity = job.Quantity;

        var competitiveRate = 0.0;

        if (viewCount < 10) return competitiveRate;

        var applicationRate = (double)applicationCount / viewCount;
        var saveRate = (double)savedCount / viewCount;

        var applyRate = quantity > 0 ? Math.Min(1.0, (double)applicationCount / quantity) : 0;
        var interviewRate = quantity > 0
            ? Math.Min(1.0, (double)(applicationShortListedCount + applicationInterviewing) / quantity)
            : 0;
        var offerRate = quantity > 0
            ? Math.Min(1.0, (double)(applicationOffered + applicationHired) / quantity)
            : 0;

        competitiveRate =
            (applicationRate * 0.15) +
            (saveRate * 0.1) +
            (applyRate * 0.2) +
            (interviewRate * 0.25) +
            (offerRate * 0.3);

        competitiveRate = Math.Min(0.9999, competitiveRate);

        return competitiveRate;
    }

    public async Task<AdminStats> GetAdminStats()
    {
        var (currentYear, currentMonth, _) = DateTime.UtcNow;
        var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

        var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var currentMonthRevenue = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed
                        && o.Created.Year == currentYear
                        && o.Created.Month == currentMonth)
            .SumAsync(o => o.Amount);

        var previousMonthRevenue = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed
                        && o.Created.Year == previousYear
                        && o.Created.Month == previousMonth)
            .SumAsync(o => o.Amount);

        var revenueRateCompared = previousMonthRevenue != 0
            ? (currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue * 100
            : 0;

        var currentJobCount = await context.Jobs
            .CountAsync(j => j.Created.Year == currentYear && j.Created.Month == currentMonth);
        var previousJobCount = await context.Jobs
            .CountAsync(j => j.Created.Year == previousYear && j.Created.Month == previousMonth);
        var jobRateCompared = previousJobCount != 0
            ? (double)(currentJobCount - previousJobCount) / previousJobCount * 100
            : 0;

        var currentCompanyCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "Business"
                        && x.Created.Year == currentYear
                        && x.Created.Month == currentMonth)
            .CountAsync();

        var previousCompanyCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "Business"
                        && x.Created.Year == previousYear
                        && x.Created.Month == previousMonth)
            .CountAsync();

        var companyRateCompared = previousCompanyCount != 0
            ? (double)(currentCompanyCount - previousCompanyCount) / previousCompanyCount * 100
            : 0;

        var currentFreelancerCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "FreelanceRecruiter"
                        && x.Created.Year == currentYear
                        && x.Created.Month == currentMonth)
            .CountAsync();

        var previousFreelancerCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "FreelanceRecruiter"
                        && x.Created.Year == previousYear
                        && x.Created.Month == previousMonth)
            .CountAsync();

        var freelancerRateCompared = previousFreelancerCount != 0
            ? (double)(currentFreelancerCount - previousFreelancerCount) / previousFreelancerCount * 100
            : 0;

        var currentCandidateCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "Candidate"
                        && x.Created.Year == currentYear
                        && x.Created.Month == currentMonth)
            .CountAsync();

        var previousCandidateCount = await context.UserRoles
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => new { ur.Name, u.Created })
            .Where(x => x.Name == "Candidate"
                        && x.Created.Year == previousYear
                        && x.Created.Month == previousMonth)
            .CountAsync();

        var candidateRateCompared = previousCandidateCount != 0
            ? (double)(currentCandidateCount - previousCandidateCount) / previousCandidateCount * 100
            : 0;

        var dailyRevenues = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed
                        && o.Created.Year == currentYear
                        && o.Created.Month == currentMonth)
            .GroupBy(o => o.Created.Day)
            .Select(g => new
            {
                Day = g.Key,
                Revenue = g.Sum(o => o.Amount)
            })
            .ToListAsync();

        var dailyRevenueList = Enumerable.Range(1, daysInMonth)
            .Select(day => new DailyRevenue
            {
                Date = new DateOnly(currentYear, currentMonth, day),
                Revenue = dailyRevenues.FirstOrDefault(dr => dr.Day == day)?.Revenue ?? 0
            })
            .ToList();

        var dailyJobCounts = await context.Jobs
            .Where(j => j.Created.Year == currentYear && j.Created.Month == currentMonth)
            .GroupBy(j => j.Created.Day)
            .Select(g => new
            {
                Day = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var dailyJobCountList = Enumerable.Range(1, daysInMonth)
            .Select(day => new DailyJobCount
            {
                Date = new DateOnly(currentYear, currentMonth, day),
                Count = dailyJobCounts.FirstOrDefault(dj => dj.Day == day)?.Count ?? 0
            })
            .ToList();

        var revenueByType = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed
                        && o.Created.Year == currentYear
                        && o.Created.Month == currentMonth)
            .GroupBy(o => o.Type)
            .Select(g => new
            {
                Type = g.Key,
                Revenue = g.Sum(o => o.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        var revenueByTypeResult = new RevenueByType
        {
            TopUpRevenue = revenueByType.FirstOrDefault(r => r.Type == OrderType.TopUp)?.Revenue ?? 0,
            TopUpCount = revenueByType.FirstOrDefault(r => r.Type == OrderType.TopUp)?.Count ?? 0,
            JobServiceRevenue = revenueByType.FirstOrDefault(r => r.Type == OrderType.Job)?.Revenue ?? 0,
            JobServiceCount = revenueByType.FirstOrDefault(r => r.Type == OrderType.Job)?.Count ?? 0,
            BusinessPackageRevenue = revenueByType.FirstOrDefault(r => r.Type == OrderType.Business)?.Revenue ?? 0,
            BusinessPackageCount = revenueByType.FirstOrDefault(r => r.Type == OrderType.Business)?.Count ?? 0
        };

        var fiveLatestOrders = await context.Orders
            .Include(o => o.User)
            .ThenInclude(u => u.CompanyDetail)
            .Where(o => o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.Created)
            .Take(5)
            .Select(o => new OrderShortResponse
            {
                Id = o.Id,
                User = new UserResponse
                {
                    Id = o.User.Id,
                    Name = o.User.CompanyDetail == null ? o.User.FullName : o.User.CompanyDetail.CompanyName,
                    ProfilePicture = o.User.CompanyDetail == null ? o.User.ProfilePicture : o.User.CompanyDetail.Logo
                },
                Amount = o.Amount,
                Type = o.Type,
                Gateway = o.Gateway,
                Status = o.Status,
                Created = o.Created
            })
            .ToListAsync();

        return new AdminStats
        {
            AdminMonthStats = new AdminMonthStats
            {
                Revenue = new Revenue
                {
                    Total = currentMonthRevenue,
                    RateCompared = Math.Round(revenueRateCompared, 2)
                },
                JobCount = new JobCount
                {
                    Count = currentJobCount,
                    RateCompared = Math.Round(jobRateCompared, 2)
                },
                CompanyCount = new CompanyCount
                {
                    Count = currentCompanyCount,
                    RateCompared = Math.Round(companyRateCompared, 2)
                },
                FreelancerCount = new FreelancerCount
                {
                    Count = currentFreelancerCount,
                    RateCompared = Math.Round(freelancerRateCompared, 2)
                },
                CandidateCount = new CandidateCount
                {
                    Count = currentCandidateCount,
                    RateCompared = Math.Round(candidateRateCompared, 2)
                }
            },
            DailyJobCounts = dailyJobCountList,
            DailyRevenues = dailyRevenueList,
            RevenueByType = revenueByTypeResult,
            FiveLatestOrder = fiveLatestOrders
        };
    }

    public async Task<RecruitmentStats> GetRecruitmentStats(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var (currentYear, currentMonth, _) = DateTime.UtcNow;
        var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);

        var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var currentMonthApplicationCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status != ApplicationStatus.Draft
                               && app.Submitted!.Value.Month == currentMonth
                               && app.Submitted!.Value.Year == currentYear);

        var previousMonthApplicationCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status != ApplicationStatus.Draft
                               && app.Submitted!.Value.Month == previousMonth
                               && app.Submitted!.Value.Year == previousYear);

        var applicationRateCompared = previousMonthApplicationCount != 0
            ? (double)(currentMonthApplicationCount - previousMonthApplicationCount) / previousMonthApplicationCount *
              100
            : 0;

        var applicationCount = new ApplicationCount
        {
            Count = currentMonthApplicationCount,
            RateCompared = Math.Round(applicationRateCompared, 2)
        };

        var currentMonthJobCount = await context.Jobs
            .Include(j => j.Campaign)
            .CountAsync(j => (j.Status == JobStatus.Active || j.Status == JobStatus.Closed)
                             && j.Created.Month == currentMonth
                             && j.Created.Year == currentYear);

        var previousMonthJobCount = await context.Jobs
            .Include(j => j.Campaign)
            .CountAsync(j => (j.Status == JobStatus.Active || j.Status == JobStatus.Closed)
                             && j.Created.Month == previousMonth
                             && j.Created.Year == previousYear);

        var jobCountRateCompared = previousMonthJobCount != 0
            ? (double)(currentMonthJobCount - previousMonthJobCount) / previousMonthJobCount * 100
            : 0;

        var jobCount = new JobCount
        {
            Count = currentMonthJobCount,
            RateCompared = Math.Round(jobCountRateCompared, 2)
        };

        var currentMonthSeenApplicationCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status == ApplicationStatus.Seen
                               && app.Submitted!.Value.Month == currentMonth
                               && app.Submitted!.Value.Year == currentYear);

        var previousMonthSeenApplicationCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status == ApplicationStatus.Seen
                               && app.Submitted!.Value.Month == previousMonth
                               && app.Submitted!.Value.Year == previousYear);

        var seenApplicationRateCompared = previousMonthSeenApplicationCount != 0
            ? (double)(currentMonthSeenApplicationCount - previousMonthSeenApplicationCount) /
            previousMonthSeenApplicationCount * 100
            : 0;

        var seenApplicationCount = new SeenApplicationCount
        {
            Count = currentMonthSeenApplicationCount,
            RateCompared = Math.Round(seenApplicationRateCompared, 2)
        };

        var currentHiredCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status != ApplicationStatus.Draft
                               && app.Process == ApplicationProcess.Hired
                               && app.Submitted!.Value.Month == currentMonth
                               && app.Submitted!.Value.Year == currentYear);

        var previousHiredCount = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status != ApplicationStatus.Draft
                               && app.Process == ApplicationProcess.Hired
                               && app.Submitted!.Value.Month == previousMonth
                               && app.Submitted!.Value.Year == previousYear);

        var currenHiredRate = currentMonthApplicationCount != 0
            ? (double)currentHiredCount / currentMonthApplicationCount * 100
            : 0;

        var previousHiredRate = previousMonthApplicationCount != 0
            ? (double)previousHiredCount / previousMonthApplicationCount * 100
            : 0;

        var hiredRateRateCompared = previousHiredRate != 0
            ? (currenHiredRate - previousHiredRate) / previousHiredRate * 100
            : 0;

        var hiredRate = new HiredRate
        {
            Rate = Math.Round(currenHiredRate, 2),
            RateCompared = Math.Round(hiredRateRateCompared, 2)
        };

        var recruitmentMonthStats = new RecruitmentMonthStats
        {
            ApplicationCount = applicationCount,
            JobCount = jobCount,
            SeenApplicationCount = seenApplicationCount,
            HiredRate = hiredRate
        };

        var allDatesInMonth = Enumerable.Range(1, daysInMonth)
            .Select(day => new DateOnly(currentYear, currentMonth, day))
            .ToList();

        var applicationsFromDb = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .Where(app => app.Job.Campaign.RecruiterId == user.Id
                          && app.Status != ApplicationStatus.Draft
                          && app.Submitted!.Value.Month == currentMonth
                          && app.Submitted!.Value.Year == currentYear)
            .GroupBy(app => DateOnly.FromDateTime(app.Submitted!.Value))
            .Select(g => new DailyApplication
            {
                Date = g.Key,
                TotalApplications = g.Count(),
                SeenApplications = g.Count(a => a.Status == ApplicationStatus.Seen)
            })
            .ToListAsync();

        var dailyApplications = allDatesInMonth
            .GroupJoin(
                applicationsFromDb,
                date => date,
                db => db.Date,
                (date, dbGroup) => new
                {
                    Date = date,
                    DbData = dbGroup.FirstOrDefault()
                })
            .Select(x => new DailyApplication
            {
                Date = x.Date,
                TotalApplications = x.DbData?.TotalApplications ?? 0,
                SeenApplications = x.DbData?.SeenApplications ?? 0
            })
            .OrderBy(da => da.Date)
            .ToList();

        var allJobs = await context.Jobs
            .Include(j => j.Campaign)
            .Include(j => j.Applications)
            .Where(j => j.Campaign.RecruiterId == user.Id
                        && j.Applications.Any(a => a.Status != ApplicationStatus.Draft
                                                   && a.Submitted!.Value.Month == currentMonth
                                                   && a.Submitted!.Value.Year == currentYear))
            .Select(j => new
            {
                j.Id,
                j.Title,
                ApplicationCount = j.Applications.Count(a => a.Status != ApplicationStatus.Draft
                                                             && a.Submitted!.Value.Month == currentMonth
                                                             && a.Submitted!.Value.Year == currentYear)
            })
            .OrderByDescending(j => j.ApplicationCount)
            .ToListAsync();

        var totalApplications = allJobs.Sum(j => j.ApplicationCount);
        var topJobs = new List<TopJob>();

        var top5Jobs = allJobs.Take(5).ToList();
        foreach (var job in top5Jobs)
        {
            topJobs.Add(new TopJob
            {
                Id = job.Id,
                Title = job.Title,
                ApplicationCount = job.ApplicationCount,
                Percentage = totalApplications > 0 ? (double)job.ApplicationCount / totalApplications * 100 : 0
            });
        }

        if (allJobs.Count > 5)
        {
            var othersCount = allJobs.Skip(5).Sum(j => j.ApplicationCount);
            if (othersCount > 0)
            {
                topJobs.Add(new TopJob
                {
                    Id = Guid.Empty,
                    Title = "Others",
                    ApplicationCount = othersCount,
                    Percentage = totalApplications > 0 ? (double)othersCount / totalApplications * 100 : 0
                });
            }
        }

        var fiveLatestApplications = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .Include(app => app.Candidate)
            .Where(app => app.Job.Campaign.RecruiterId == user.Id
                          && app.Status != ApplicationStatus.Draft)
            .OrderByDescending(app => app.Submitted)
            .Take(5)
            .Select(app => new ShortApplication
            {
                Id = app.Id,
                Candidate = new UserResponse
                {
                    Id = app.Candidate.Id,
                    Name = app.Candidate.FullName,
                    ProfilePicture = app.Candidate.ProfilePicture
                },
                JobTitle = app.Job.Title,
                CampaignId = app.Job.Campaign.Id,
                Process = app.Process,
                Submitted = app.Submitted
            })
            .ToListAsync();

        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
        var applicationCountInLatestWeek = await context.Applications
            .Include(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .CountAsync(app => app.Job.Campaign.RecruiterId == user.Id
                               && app.Status != ApplicationStatus.Draft
                               && app.Submitted >= oneWeekAgo);

        return new RecruitmentStats
        {
            RecruitmentMonthStats = recruitmentMonthStats,
            DailyApplications = dailyApplications,
            TopJobs = topJobs,
            FiveLatestApplications = fiveLatestApplications,
            ApplicationCountInLatestWeek = applicationCountInLatestWeek
        };
    }
}