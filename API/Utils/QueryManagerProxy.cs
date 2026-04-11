namespace SAMonitor.Utils;

public static class QueryManagerProxy
{
    private static readonly HttpClient Client = new();
    public static string? ProxyUrl { get; private set; }

    public static async Task<bool> SetupAsync()
    {
        try
        {
            if (!File.Exists("query_service.txt")) return true;

            ProxyUrl = (await File.ReadAllTextAsync("query_service.txt")).Trim();

            if (string.IsNullOrEmpty(ProxyUrl)) return true;

            if (!ProxyUrl.StartsWith("http"))
            {
                ProxyUrl = "http://" + ProxyUrl;
            }

            Console.WriteLine($"Query Proxy Service loaded: {ProxyUrl}");
            Console.WriteLine("Pinging Query Proxy Service...");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Client.GetAsync($"{ProxyUrl}/query", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Query Proxy Service is online.");
                return true;
            }

            Console.WriteLine($"Query Proxy Service returned an error: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or connect to Query Proxy Service: {ex.Message}");
            return false;
        }
    }
}
