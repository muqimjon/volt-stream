namespace VoltStream.WebApi.Utils;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

public class SimpleDiscoveryResponder(IServer server, IHostEnvironment env, IConfiguration config, ILogger<SimpleDiscoveryResponder> logger) : BackgroundService
{
    private const int ListenPort = 5001;
    private readonly IServerAddressesFeature? _serverAddresses = server.Features.Get<IServerAddressesFeature>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var udp = new UdpClient(ListenPort);
        logger.LogInformation("📡 Discovery responder listening on UDP port {port}", ListenPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(stoppingToken);
                var msg = Encoding.UTF8.GetString(result.Buffer).Trim();

                if (msg != "DISCOVER")
                    continue;

                var ip = ResolveIp();
                var response = $"{ResolveScheme()}://{ip}:{ResolvePort()}";

                var bytes = Encoding.UTF8.GetBytes(response);
                await udp.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
                logger.LogInformation("✅ Discovery response: {response} → {remote}", response, result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Discovery responder error: {msg}", ex.Message);
                await Task.Delay(100, stoppingToken);
            }
        }
    }

    private string ResolveScheme()
    {
        var urls = config["ASPNETCORE_URLS"];
        if (urls?.Contains("https://") == true) return "https";
        if (urls?.Contains("http://") == true) return "http";

        var raw = _serverAddresses?.Addresses.FirstOrDefault();
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) ? uri.Scheme : "http";
    }

    private string ResolvePort()
    {
        // Docker-specific override
        var dockerHttp = config["ASPNETCORE_HTTP_PORTS"];
        var dockerHttps = config["ASPNETCORE_HTTPS_PORTS"];
        var dockerPort = config["DISCOVERY_PORT"];

        if (!string.IsNullOrWhiteSpace(dockerPort))
            return dockerPort;
        if (!string.IsNullOrWhiteSpace(dockerHttps))
            return dockerHttps;
        if (!string.IsNullOrWhiteSpace(dockerHttp))
            return dockerHttp;

        // Fallback to ASPNETCORE_URLS
        var urls = config["ASPNETCORE_URLS"];
        var uri = urls?.Split(';').FirstOrDefault();
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            return parsed.Port.ToString();

        // Fallback to IServerAddressesFeature
        var raw = _serverAddresses?.Addresses.FirstOrDefault();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var fallback))
            return fallback.Port.ToString();

        return "7285"; // default
    }


    private string ResolveIp()
    {
        // 1) Ixtiyoriy qo'lda override (faqat zarur bo'lsa). Odatda KERAK EMAS — avtomatik ishlaydi.
        var advertise = config["Discovery:AdvertiseIp"]
                     ?? config["DISCOVERY_ADVERTISE_IP"]
                     ?? config["ADVERTISE_IP"];
        if (!string.IsNullOrWhiteSpace(advertise) && IPAddress.TryParse(advertise.Trim(), out _))
            return advertise.Trim();

        if (env.IsDevelopment())
            return "localhost";

        // 2) Avtomatik: WiFi/LAN tarmog'idagi tashqi IPv4 (host yoki host-network rejimida ishlaydi).
        //    Har so'rovda qayta hisoblanadi → server IP'si o'zgarsa (DHCP) ham mos keladi.
        return GetLanIp() ?? GetOutboundIp() ?? "localhost";
    }

    // Faol jismoniy interfeys (Ethernet/Wi-Fi) dan private IPv4 ni tanlaydi;
    // virtual/Docker/WSL adapterlarini chetlab o'tib, WiFi/LAN diapazonini
    // (192.168.* eng avval, keyin 10.*) afzal ko'radi.
    private static string? GetLanIp()
    {
        try
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                                                      or NetworkInterfaceType.Wireless80211)
                .Where(ni => !ni.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase)
                          && !ni.Description.Contains("docker", StringComparison.OrdinalIgnoreCase)
                          && !ni.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase)
                          && !ni.Name.Contains("WSL", StringComparison.OrdinalIgnoreCase)
                          && !ni.Name.StartsWith("docker", StringComparison.OrdinalIgnoreCase)
                          && !ni.Name.StartsWith("br-", StringComparison.OrdinalIgnoreCase)
                          && !ni.Name.StartsWith("veth", StringComparison.OrdinalIgnoreCase))
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Select(ua => ua.Address)
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                .Select(ip => ip.ToString())
                .Where(ip => !ip.StartsWith("169.254"))
                .ToList();

            return addresses.FirstOrDefault(ip => ip.StartsWith("192.168."))
                ?? addresses.FirstOrDefault(ip => ip.StartsWith("10."))
                ?? addresses.FirstOrDefault(IsPrivate172)
                ?? addresses.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPrivate172(string ip)
    {
        // 172.16.0.0 – 172.31.255.255 (Docker shu diapazondan foydalanadi — eng oxirgi tanlov)
        var parts = ip.Split('.');
        return parts.Length == 4 && parts[0] == "172"
            && int.TryParse(parts[1], out var second) && second is >= 16 and <= 31;
    }

    // Tashqi manzilga "ulanib" (haqiqiy paket yubormasdan) qaysi local IPv4 chiqishini aniqlaydi.
    // Host yoki host-network rejimida WiFi IP'sini beradi.
    private static string? GetOutboundIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch
        {
            return null;
        }
    }
}
