namespace VoltStream.WebApi.Utils;

using System.Net;

public static class IpHelper
{
    private static readonly Lazy<IPAddress[]> localIps = new(() => Dns.GetHostAddresses(Dns.GetHostName()));

    public static bool IsLocal(IPAddress? ipAddress)
    {
        if (ipAddress is null) return false;
        if (IPAddress.IsLoopback(ipAddress)) return true;

        return localIps.Value.Contains(ipAddress);
    }
}
