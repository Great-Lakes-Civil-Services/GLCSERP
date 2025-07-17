public class UserActivityLog
{
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
}
