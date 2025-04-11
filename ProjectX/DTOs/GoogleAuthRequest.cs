namespace ProjectX.DTOs;

public class GoogleAuthRequest
{
    public required string RoleName { get; set; }
    public required string IdToken { get; set; }
}