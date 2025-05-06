using System.Security.Cryptography;

namespace ProjectX.Helpers;

public static class PasswordHelper
{
    public static string GenerateCompliantPassword(int requiredLength = 12)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_-+=<>?";

        var rng = RandomNumberGenerator.Create();
        var passwordChars = new List<char>
        {
            GetRandomChar(upper, rng),
            GetRandomChar(lower, rng),
            GetRandomChar(digits, rng),
            GetRandomChar(special, rng)
        };

        const string allChars = upper + lower + digits + special;
        while (passwordChars.Count < requiredLength)
        {
            passwordChars.Add(GetRandomChar(allChars, rng));
        }

        return new string(passwordChars.OrderBy(_ => Guid.NewGuid()).ToArray());
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var bytes = new byte[1];
        rng.GetBytes(bytes);
        return chars[bytes[0] % chars.Length];
    }
}