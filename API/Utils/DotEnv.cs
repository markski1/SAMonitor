namespace SAMonitor.Utils;

public static class DotEnv
{
    public static void Load()
    {
        string[] paths = [AppContext.BaseDirectory, Directory.GetCurrentDirectory()];

        foreach (var basePath in paths)
        {
            var filePath = Path.Combine(basePath, ".env");
            if (!File.Exists(filePath)) continue;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#') continue;

                var idx = trimmed.IndexOf('=');
                if (idx <= 0) continue;

                var key = trimmed[..idx].Trim();
                var value = trimmed[(idx + 1)..];

                if (value.Length > 1 && value[0] == '"' && value[^1] == '"')
                {
                    value = value[1..^1];
                }
                else if (value.Length > 1 && value[0] == '\'' && value[^1] == '\'')
                {
                    value = value[1..^1];
                }
                else
                {
                    var commentIdx = value.IndexOf(" #");
                    if (commentIdx >= 0) value = value[..commentIdx];
                    value = value.TrimEnd();
                }

                Environment.SetEnvironmentVariable(key, value);
            }

            Console.WriteLine($"Loaded .env from {filePath}");
            return;
        }
    }
}
