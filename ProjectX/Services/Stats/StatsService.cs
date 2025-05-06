using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
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
}