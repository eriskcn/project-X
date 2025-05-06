using ProjectX.Models;

namespace ProjectX.Services.Competitive;

public interface ICompetitiveRateService
{
    Task<double> GetCompetitiveRateAsync(Job job);
}