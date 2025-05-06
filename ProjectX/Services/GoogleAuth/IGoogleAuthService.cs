using Google.Apis.Auth;

namespace ProjectX.Services.GoogleAuth;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken);
}