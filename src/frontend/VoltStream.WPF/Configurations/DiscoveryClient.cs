namespace VoltStream.WPF.Configurations;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

public class DiscoveryClient
{
    private const int DiscoveryPort = 5001;
    private const int BroadcastTimeoutMs = 1500;
    private const int BroadcastAttempts = 2;

    // Server qaysi portda bo'lishi mumkin:
    //   HTTP : 8080 — Docker (production), 5000 — native production, 5123 — dev (VS http profili).
    //   HTTPS: 7285 — dev (VS https profili), 8081 — Docker https.
    // Boshqa port ishlatilsa, mos ro'yxatga qo'shing.
    private static readonly int[] HttpPorts = [8080, 5000, 5123];
    private static readonly int[] HttpsPorts = [7285, 8081];

    private const int ProbeTimeoutMs = 900;
    private const int AliveTimeoutMs = 1500;
    private const int MaxParallelProbes = 100;

    // Diagnostika: ishlash papkasiga (VS'da bin\Debug\...) yoziladi.
    private const string LogFile = "discovery.log";

    /// <summary>
    /// Serverni avtomatik topadi. UDP broadcast (dev/host tarmoq) va LAN skan
    /// (Docker/bridge yoki broadcast bloklanganda) BIR VAQTDA ishlaydi — birinchi
    /// haqiqiy (tirik) natija g'olib. Topilmasa null.
    /// </summary>
    public static async Task<Uri?> DiscoverAsync()
    {
        Log("DiscoverAsync boshlandi (broadcast + skan parallel)");

        using var cts = new CancellationTokenSource();
        var broadcastTask = VerifiedBroadcastAsync();
        var scanTask = ScanLanAsync(cts.Token);

        var pending = new List<Task<Uri?>> { broadcastTask, scanTask };
        while (pending.Count > 0)
        {
            var done = await Task.WhenAny(pending);
            pending.Remove(done);

            Uri? result = null;
            try { result = await done; } catch { }

            if (result is not null)
            {
                cts.Cancel(); // ikkinchisini to'xtatamiz
                Log($"TOPILDI: {result}");
                return result;
            }
        }

        Log("Topilmadi (broadcast ham, skan ham natija bermadi)");
        return null;
    }

    /// <summary>
    /// Berilgan URL'dagi server hali javob beryaptimi? (saqlangan URL'ni tez tekshirish uchun).
    /// example.com kabi soxta standart manzilga ulanib osilib qolmaslik uchun qisqa timeout bilan.
    /// </summary>
    public static async Task<bool> IsAliveAsync(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Host.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            using var handler = CreateHandler();
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(AliveTimeoutMs) };
            using var response = await http.GetAsync($"{uri.GetLeftPart(UriPartial.Authority)}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Broadcast javobiga FAQAT haqiqatan tirik bo'lsa ishonamiz (responder eski/noto'g'ri
    // sxema yoki port reklama qilishi mumkin).
    private static async Task<Uri?> VerifiedBroadcastAsync()
    {
        var uri = await TryBroadcastAsync();
        if (uri is null)
        {
            Log("Broadcast: javob yo'q");
            return null;
        }

        var alive = await IsAliveAsync(uri.ToString());
        Log($"Broadcast javobi: {uri}  (tirik: {alive})");
        return alive ? uri : null;
    }

    private static async Task<Uri?> TryBroadcastAsync()
    {
        for (int attempt = 1; attempt <= BroadcastAttempts; attempt++)
        {
            try
            {
                using var udp = new UdpClient { EnableBroadcast = true };
                var request = Encoding.UTF8.GetBytes("DISCOVER");
                await udp.SendAsync(request, request.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                var receiveTask = udp.ReceiveAsync();
                var completed = await Task.WhenAny(receiveTask, Task.Delay(BroadcastTimeoutMs));
                if (completed == receiveTask)
                {
                    var response = Encoding.UTF8.GetString(receiveTask.Result.Buffer).Trim();
                    if (Uri.TryCreate(response, UriKind.Absolute, out var uri))
                        return uri;
                }
            }
            catch { }
        }

        return null;
    }

    private static async Task<Uri?> ScanLanAsync(CancellationToken external = default)
    {
        // Skan manzillari: avval "localhost" (dev — server o'sha kompyuterda; "localhost"
        // .NET tomonidan IPv4 127.0.0.1 va IPv6 ::1 ga ham uriniladi). Faqat "localhost"
        // ishlatamiz (raw 127.0.0.1 emas) — chunki dev HTTPS sertifikati "localhost" nomiga
        // berilgan, IP'ga emas. Keyin WiFi/LAN subneti (production — http, sertifikatsiz).
        var targets = new List<string> { "localhost" };
        var prefixes = GetLocalSubnetPrefixes();
        foreach (var prefix in prefixes)
            for (int host = 1; host <= 254; host++)
                targets.Add($"{prefix}.{host}");

        Log($"Skan: subnetlar=[{string.Join(", ", prefixes)}], manzillar={targets.Count}");

        // Har manzil uchun mos sxema bilan tekshiriladigan to'liq base-URL'lar.
        var baseUrls = new List<string>();
        foreach (var t in targets)
        {
            foreach (var port in HttpPorts)
                baseUrls.Add($"http://{t}:{port}");
            foreach (var port in HttpsPorts)
                baseUrls.Add($"https://{t}:{port}");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
        using var handler = CreateHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(ProbeTimeoutMs) };
        using var gate = new SemaphoreSlim(MaxParallelProbes);

        var tasks = new List<Task<Uri?>>();
        foreach (var baseUrl in baseUrls)
            tasks.Add(ProbeAsync(http, baseUrl, gate, cts.Token));

        // Birinchi javob bergan server topilishi bilan qaytamiz, qolganini bekor qilamiz.
        var remaining = new List<Task<Uri?>>(tasks);
        while (remaining.Count > 0)
        {
            var finished = await Task.WhenAny(remaining);
            remaining.Remove(finished);

            Uri? result = null;
            try { result = await finished; } catch { }
            if (result is not null)
            {
                cts.Cancel();
                Log($"Skan topdi: {result}");
                return result;
            }
        }

        return null;
    }

    private static async Task<Uri?> ProbeAsync(HttpClient http, string baseUrl, SemaphoreSlim gate, CancellationToken ct)
    {
        try
        {
            await gate.WaitAsync(ct);
            try
            {
                using var response = await http.GetAsync($"{baseUrl}/api/health", ct);
                if (response.IsSuccessStatusCode)
                {
                    // Redirect bo'lgan bo'lsa (masalan http→https), YAKUNIY manzilni qaytaramiz.
                    var authority = response.RequestMessage?.RequestUri?.GetLeftPart(UriPartial.Authority) ?? baseUrl;
                    return new Uri(authority + "/");
                }
            }
            finally
            {
                gate.Release();
            }
        }
        catch { } // timeout / ulanmadi / sxema mos emas / bekor qilindi — bu manzilda server yo'q

        return null;
    }

    // LAN ichidagi avtomatik kashf uchun self-signed (dev) sertifikatga bardosh handler.
    private static HttpClientHandler CreateHandler()
        => new()
        {
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
            AllowAutoRedirect = true
        };

    // Mahalliy faol (Ethernet/Wi-Fi) interfeyslardan /24 subnet prefiksini oladi (masalan "192.168.1").
    private static List<string> GetLocalSubnetPrefixes()
    {
        var prefixes = new List<string>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType is not (NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211))
                    continue;
                if (ni.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase)
                 || ni.Description.Contains("docker", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    var ip = ua.Address;
                    if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                        continue;

                    var s = ip.ToString();
                    if (s.StartsWith("169.254"))
                        continue;

                    var prefix = string.Join('.', s.Split('.').Take(3));
                    if (!prefixes.Contains(prefix))
                        prefixes.Add(prefix);
                }
            }
        }
        catch { }

        return prefixes;
    }

    // Tashqaridan (App startup) diagnostika yozish uchun.
    public static void Diag(string message) => Log(message);

    // Har ishga tushishda log faylni yangilaydi (cheksiz o'smasligi uchun).
    public static void ResetDiag()
    {
        try { File.WriteAllText(LogFile, $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} discovery ==={Environment.NewLine}"); }
        catch { }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }
        catch { }
    }
}
