using System.ComponentModel.DataAnnotations;

namespace SAMonitor.Data;

public enum ServerSubmissionOutcome
{
    Added,
    AlreadyMonitored,
    RecentlyFailed,
    Blacklisted,
    Unresponsive,
    Unsupported,
    Duplicate,
    Failed
}

public sealed record ServerSubmissionResult(ServerSubmissionOutcome Outcome, string Message);

public sealed class AddServerRequest
{
    [Required]
    public string IpAddr { get; init; } = "";
}

public sealed record AddServerResponse(string IpAddr, string Message);
