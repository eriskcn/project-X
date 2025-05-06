using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Services.Competitive;

public class CompetitiveRateService(ApplicationDbContext context) : ICompetitiveRateService
{
    public async Task<double> GetCompetitiveRateAsync(Job job)
    {
        var applications = await context.Applications
            .Where(x => x.JobId == job.Id && x.Status != ApplicationStatus.Draft)
            .ToListAsync();

        var viewCount = job.ViewCount;
        return 0.0;
    }
}