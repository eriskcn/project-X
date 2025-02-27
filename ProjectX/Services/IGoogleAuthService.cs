using Google.Apis.Auth;

namespace ProjectX.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken);
}