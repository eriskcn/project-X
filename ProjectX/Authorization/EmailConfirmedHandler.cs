using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;

namespace ProjectX.Authorization;

public class EmailConfirmedHandler(ApplicationDbContext dbContext) : AuthorizationHandler<EmailConfirmedRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        EmailConfirmedRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
        {
            context.Fail();
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            context.Fail();
            return;
        }

        if (user.EmailConfirmed)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}