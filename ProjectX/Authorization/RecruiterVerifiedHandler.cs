using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjectX.Authorization;

public class RecruiterVerifiedHandler : AuthorizationHandler<RecruiterVerifiedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        RecruiterVerifiedRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        if (roles.Contains("Business") || roles.Contains("FreelanceRecruiter"))
        {
            var recruiterVerifiedClaim = context.User.FindFirst("RecruiterVerified")?.Value;
            var isRecruiterVerified = recruiterVerifiedClaim != null && bool.Parse(recruiterVerifiedClaim);

            if (isRecruiterVerified)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        else
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}