using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Globalization;
using System.Text.Json;
using System.Threading;

namespace SAMonitor.Utils;

public static class Helpers
{
    public static bool IsDevelopment = false;

    private static string WebhookUrl = "";
    private static readonly HttpClient _httpClient = new();

    // Debounce to stop hitting Discord limits.
    private static readonly TimeSpan DiscordMinInterval = TimeSpan.FromSeconds(60);
    private static readonly Lock DiscordLock = new();
    private static readonly Dictionary<string, DateTime> DiscordLastSent = new(StringComparer.Ordinal);

    public static void LoadWebhookUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            WebhookUrl = envUrl;
        }
    }

    public static async Task<string> ValidateIPv4(string ipAddr)
    {
        return await ValidateIPv4(ipAddr, Dns.GetHostAddressesAsync);
    }

    internal static async Task<string> ValidateIPv4(string ipAddr, Func<string, Task<IPAddress[]>> resolve)
    {
        var parts = ipAddr.Trim().Split(':');
        if (parts.Length is < 1 or > 2 || string.IsNullOrWhiteSpace(parts[0])) return "invalid";

        int port = 7777;
        if (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535))
        {
            return "invalid";
        }

        IPAddress? address;
        string host = parts[0];
        if (host.All(c => char.IsAsciiDigit(c) || c == '.'))
        {
            var octets = host.Split('.');
            if (octets.Length != 4 || octets.Any(x => !byte.TryParse(x, NumberStyles.None, CultureInfo.InvariantCulture, out _))) return "invalid";
            address = new IPAddress(octets.Select(x => byte.Parse(x, CultureInfo.InvariantCulture)).ToArray());
        }
        else
        {
            if (Uri.CheckHostName(host) != UriHostNameType.Dns) return "invalid";
            try
            {
                address = (await resolve(host)).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return "invalid";
            }
        }

        return address is null ? "invalid" : $"{address}:{port}";
    }

    public static string BodgedEncodingFix(string text)
    {
        text = text.Replace('с', 'ñ');
        text = text.Replace('к', 'ê');
        text = text.Replace('Ў', '¡');
        text = text.Replace('У', 'Ó');
        text = text.Replace('у', 'ó');
        text = text.Replace('б', 'á');
        return text;
    }

    public static async Task LogError(string context, Exception ex)
    {
        // Temporal but while some issues in the new server are being ironed out I want to know when and why db ops fail.
        try
        {
            var message = $"[err] {context} \n {ex}";
            Console.WriteLine(message);
            await File.AppendAllTextAsync("log.txt", $"{message}\n");

            if (!IsDevelopment)
            {
                await SendDiscordMessage(context, message);
            }
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"[log error] Failed to write to log.txt or send Discord message: {logEx.Message}");
        }
    }

    private static async Task SendDiscordMessage(string context, string message)
    {
        bool shouldSend;
        lock (DiscordLock)
        {
            var now = DateTime.UtcNow;
            if (DiscordLastSent.TryGetValue(context, out var last) && now - last < DiscordMinInterval)
            {
                shouldSend = false;
            }
            else
            {
                DiscordLastSent[context] = now;
                shouldSend = true;
            }
        }

        if (!shouldSend)
        {
            // nothing to do here.
            return;
        }

        try
        {
            var payload = new { content = message.Length > 2000 ? message[..1997] + "..." : message };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync(WebhookUrl, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send Discord message: {ex.Message}");
        }
    }
}
