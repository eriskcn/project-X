using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjectX.Authorization;

public class BusinessVerifiedHandler : AuthorizationHandler<BusinessVerifiedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        BusinessVerifiedRequirement requirement)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        if (roles.Contains("Business"))
        {
            var businessVerifiedClaim = context.User.FindFirst("BusinessVerified")?.Value;
            var isBusinessVerified = businessVerifiedClaim != null && bool.Parse(businessVerifiedClaim);

            if (isBusinessVerified)
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