namespace Infrastructure.Settings;

public class EmailSettings
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderPassword { get; set; }
    public string? SenderName { get; set; }
}