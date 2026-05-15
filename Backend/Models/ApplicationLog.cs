using System.ComponentModel.DataAnnotations;

namespace JobApplicationAssistant.Api.Models;

public class ApplicationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid JobApplicationId { get; set; }
    public JobApplication? JobApplication { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? ScreenshotLocalPath { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public LogLevel Level { get; set; } = LogLevel.Information;
}
