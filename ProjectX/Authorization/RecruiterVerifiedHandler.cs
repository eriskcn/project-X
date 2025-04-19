using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;

namespace ProjectX.Authorization;

public class RecruiterVerifiedHandler(ApplicationDbContext dbContext)
    : AuthorizationHandler<RecruiterVerifiedRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authContext,
        RecruiterVerifiedRequirement requirement)
    {
        var userId = authContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
        {
            authContext.Fail();
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            authContext.Fail();
            return;
        }

        var roles = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        if (!(roles.Contains("Business") || roles.Contains("FreelanceRecruiter")))
        {
            authContext.Fail();
            return;
        }

        if (user.RecruiterVerified)
        {
            authContext.Succeed(requirement);
        }
        else
        {
            authContext.Fail();
        }
    }
}