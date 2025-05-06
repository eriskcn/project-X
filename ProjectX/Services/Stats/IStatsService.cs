using ProjectX.Models;

namespace ProjectX.Services.Stats;

public interface IStatsService
{
    Task<double> GetCompetitiveRateAsync(Job job);
}