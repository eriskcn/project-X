using ProjectX.DTOs.Stats;
using ProjectX.Models;

namespace ProjectX.Services.Stats;

public interface IStatsService
{
    Task<double> GetCompetitiveRateAsync(Job job);
    Task<AdminStats> GetAdminStats();
    Task<RecruitmentStats> GetRecruitmentStats(User user);
}