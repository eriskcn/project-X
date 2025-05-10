namespace ProjectX.DTOs.Turnstiles;

public class TurnstileVerifyResponse
{
    public bool Success { get; set; }
    public string[] ErrorCodes { get; set; }
}