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
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // Har bir datagramma qaysi LOCAL (qabul qiluvchi) IP'ga kelganini bilish uchun:
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, ListenPort));

        logger.LogInformation("📡 Discovery responder listening on UDP port {port}", ListenPort);

        var buffer = new byte[1024];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                var result = await socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, remote, stoppingToken);
                var msg = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes).Trim();

                if (msg != "DISCOVER")
                    continue;

                // result.PacketInformation.Address = DISCOVER aynan qaysi local IP'ga kelgani.
                // Bu — client serverga ulanish uchun ishlatgan IP, demak kafolatlangan to'g'ri manzil.
                var ip = ResolveIp(result.PacketInformation.Address);
                var response = $"{ResolveScheme()}://{ip}:{ResolvePort()}";

                var bytes = Encoding.UTF8.GetBytes(response);
                await socket.SendToAsync(bytes, SocketFlags.None, result.RemoteEndPoint, stoppingToken);
                logger.LogInformation("✅ Discovery response: {response} → {remote} (qabul qilingan IP: {local})",
                    response, result.RemoteEndPoint, result.PacketInformation.Address);
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


    private string ResolveIp(IPAddress? receivedOn)
    {
        // 1) Ixtiyoriy qo'lda override (faqat zarur bo'lsa). Odatda KERAK EMAS — avtomatik ishlaydi.
        var advertise = config["Discovery:AdvertiseIp"]
                     ?? config["DISCOVERY_ADVERTISE_IP"]
                     ?? config["ADVERTISE_IP"];
        if (!string.IsNullOrWhiteSpace(advertise) && IPAddress.TryParse(advertise.Trim(), out _))
            return advertise.Trim();

        if (env.IsDevelopment())
            return "localhost";

        // 2) ENG ISHONCHLI: DISCOVER aynan shu local IP'ga kelgan — ya'ni client serverga
        //    aynan shu manzil orqali yetib kelgan. Demak bu IP o'sha client uchun kafolatlangan
        //    to'g'ri (WiFi/LAN) manzil. Har so'rovda qayta hisoblanadi → IP o'zgarsa ham mos keladi.
        if (IsUsableLanIp(receivedOn))
            return receivedOn!.ToString();

        // 3) Zaxira: tarmoq interfeyslaridan WiFi/LAN IPv4 ni avtomatik tanlash.
        return GetLanIp() ?? GetOutboundIp() ?? "localhost";
    }

    // Loopback, link-local (169.254) va Docker-ichki (172.16–31) bo'lmagan, haqiqiy LAN IPv4mi?
    private static bool IsUsableLanIp(IPAddress? ip)
    {
        if (ip is null || ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
            return false;
        var s = ip.ToString();
        return !s.StartsWith("169.254") && !IsPrivate172(s);
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
