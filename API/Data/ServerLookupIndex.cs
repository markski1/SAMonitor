namespace SAMonitor.Data;

internal sealed class ServerLookupIndex
{
    private readonly Dictionary<string, Server> _byExactIp;
    private readonly Dictionary<string, Server> _byHostPrimary;

    private ServerLookupIndex(Dictionary<string, Server> byExactIp, Dictionary<string, Server> byHostPrimary)
    {
        _byExactIp = byExactIp;
        _byHostPrimary = byHostPrimary;
    }

    internal static ServerLookupIndex Create(IReadOnlyList<Server> servers)
    {
        var byExactIp = new Dictionary<string, Server>(servers.Count, StringComparer.OrdinalIgnoreCase);
        var byHostPrimary = new Dictionary<string, Server>(servers.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var server in servers)
        {
            byExactIp[server.IpAddr] = server;

            var host = GetHostPart(server.IpAddr);
            if (!byHostPrimary.TryGetValue(host, out var existing))
            {
                byHostPrimary[host] = server;
                continue;
            }

            if (!IsDefaultPort(existing.IpAddr) && IsDefaultPort(server.IpAddr))
            {
                byHostPrimary[host] = server;
            }
        }

        return new ServerLookupIndex(byExactIp, byHostPrimary);
    }

    internal Server? Lookup(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;

        ip = ip.Trim();
        if (_byExactIp.TryGetValue(ip, out var exact))
        {
            return exact;
        }

        if (ip.Contains(':')) return null;

        return _byHostPrimary.GetValueOrDefault(ip);
    }

    internal static string GetHostPart(string ip)
    {
        int colonIndex = ip.IndexOf(':');
        return colonIndex >= 0 ? ip[..colonIndex] : ip;
    }

    internal static bool IsDefaultPort(string ip)
    {
        int colonIndex = ip.IndexOf(':');
        if (colonIndex < 0 || colonIndex == ip.Length - 1) return false;
        return ip[(colonIndex + 1)..] == "7777";
    }
}
