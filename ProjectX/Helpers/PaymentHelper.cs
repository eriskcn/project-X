using System.Text.RegularExpressions;

namespace ProjectX.Helpers;

public class PaymentHelper
{
    public static Guid? ExtractGuid(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var regex = new Regex(@"PAY(?<id>[a-fA-F0-9]{32})", RegexOptions.IgnoreCase);
        var match = regex.Match(content);

        if (!match.Success) return null;
        var rawGuid = match.Groups["id"].Value;
        try
        {
            return Guid.ParseExact(rawGuid, "N");
        }
        catch
        {
            return null;
        }
    }
}