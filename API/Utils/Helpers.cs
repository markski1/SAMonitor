using System.Net;
using System.Text;
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
        bool needsResolving = false;
        var items = ipAddr.Split('.');
        if (items.Length != 4 || ipAddr.Any(char.IsLetter))
        {
            // if it doesn't look like a valid IP address, check it has any dots at all.
            if (items.Length <= 0)
            {
                return "invalid";
            }
            // if it does, then assume it's a hostname and must be resolved.
            needsResolving = true;
        }

        // separate by port, if any.
        items = ipAddr.Split(':');

        // if we need to resolve, items[0] will be the hostname.
        if (needsResolving)
        {
            try
            {
                // resolve hostname to ip address and assign
                var hostEntry = await Dns.GetHostEntryAsync(items[0]);
                ipAddr = hostEntry.AddressList[0].ToString();
                // fill in the port if provided, else 7777
                ipAddr = items.Length != 2 ? $"{ipAddr}:7777" : $"{ipAddr}:{items[1]}";
            }
            catch
            {
                return "invalid";
            }
        }
        // if we don't need to resolve, then just make sure there's a port.
        else if (items.Length != 2)
        {
            ipAddr = $"{ipAddr}:7777";
        }

        return ipAddr;
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
