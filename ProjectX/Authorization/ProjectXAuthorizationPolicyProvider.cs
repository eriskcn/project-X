using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ProjectX.Authorization;

public class ProjectXAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    private readonly AuthorizationOptions _options = options.Value;

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var basePolicy = await base.GetPolicyAsync(policyName);

        if (basePolicy == null) return null;

        var builder = new AuthorizationPolicyBuilder(basePolicy);
        builder.Requirements.Add(new EmailConfirmedRequirement());

        return builder.Build();
    }
}