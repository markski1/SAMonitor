using System.Net;

namespace SAMonitor.Utils
{
    public static class Helpers
    {
        public static string ValidateIPv4(string ip_addr)
        {
            bool needsResolving = false;
            var items = ip_addr.Split('.');
            if (items.Length != 4 || ip_addr.Any(char.IsLetter))
            {
                // if it doesn't look like a valid IP address, check it has any dots at all.
                if (items.Length < 0)
                {
                    return "invalid";
                }
                // if it does, then assume it's a hostname and must be resolved.
                needsResolving = true;
            }

            // separate by port, if any.
            items = ip_addr.Split(':');

            // if we need to resolve, items[0] will be the hostname.
            if (needsResolving)
            {
                try
                {
                    // resolve hostname to ip address and assign
                    var hostEntry = Dns.GetHostEntry(items[0]);
                    ip_addr = hostEntry.AddressList[0].ToString();
                    // fill in the port if provided, else 7777
                    if (items.Length != 2) ip_addr = $"{ip_addr}:7777";
                    else ip_addr = $"{ip_addr}:{items[1]}";
                }
                catch
                {
                    return "invalid";
                }
            }
            // if we don't need to resolve, then just make sure there's a port.
            else if (items.Length != 2)
            {
                ip_addr = $"{ip_addr}:7777";
            }

            return ip_addr;
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
    }
}