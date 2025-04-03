using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Models;
using Microsoft.AspNetCore.Identity;

namespace ProjectX.Services;

public class TokenService(IConfiguration configuration, UserManager<User> userManager)
    : ITokenService
{
    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("BusinessVerified", user.BusinessVerified.ToString())
        };

        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                                   throw new ArgumentNullException("Jwt:Key",
                                       "The JWT secret key is not configured.")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    public bool IsAccessTokenExpired(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (tokenHandler.ReadToken(accessToken) is not JwtSecurityToken jwtToken)
            throw new ArgumentException("Invalid access token");

        var expirationDate = jwtToken.ValidTo;
        return expirationDate < DateTime.UtcNow;
    }

    public bool IsBusinessVerified(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (tokenHandler.ReadToken(accessToken) is not JwtSecurityToken jwtToken)
            throw new ArgumentException("Invalid access token");

        var businessVerified = jwtToken.Claims.FirstOrDefault(c => c.Type == "BusinessVerified")?.Value;
        return businessVerified == "True";
    }
}