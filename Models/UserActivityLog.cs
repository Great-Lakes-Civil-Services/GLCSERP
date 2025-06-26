public class UserActivityLog
{
    public DateTime Timestamp { get; set; }
    public string Username { get; set; }
    public string Action { get; set; }
    public string Detail { get; set; }
    public string ChangedBy { get; set; }
}
