using System.Net.Sockets;

namespace ProjectX.Helpers;

public class SafeNetworkHelper
{
    public static string GetClientIp(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ip = context.Connection.RemoteIpAddress;

        if (ip == null)
            throw new NullReferenceException("IP address not found");
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            ip = ip.MapToIPv4();
        }

        return ip.ToString();
    }
}