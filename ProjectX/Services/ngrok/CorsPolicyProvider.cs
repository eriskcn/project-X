namespace ProjectX.Services.ngrok;

public class CorsPolicyProvider(NgrokService ngrokService)
{
    public async Task<string> GetAllowOriginAsync()
    {
        var ngrokUrl = await ngrokService.GetNgrokUrlAsync();
        return ngrokUrl;
    }
}