using ProjectX.Models;

namespace ProjectX.Services;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    string GenerateRefreshToken();
}