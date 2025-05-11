using System.Net;

namespace SAMonitor.Utils;

public static class Helpers
{
    public static bool IsDevelopment = false;

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

    public static void LogError(string context, Exception ex)
    {
        try
        {
            Console.WriteLine($"[err] {context} \n {ex}");
            File.AppendAllText("log.txt", $"[err] {context} \n {ex}");
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"[log error] Failed to write to log.txt: {logEx.Message}");
        }
    }
}