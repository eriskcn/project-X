using Google.Apis.Auth;

namespace ProjectX.Services;

public class GoogleAuthService
{
    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            return payload;
        }
        catch (Exception ex)
        {
            throw new Exception("Invalid Google Token", ex);
        }
    }
}