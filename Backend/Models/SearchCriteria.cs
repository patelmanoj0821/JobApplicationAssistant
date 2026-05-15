using System.ComponentModel.DataAnnotations;

namespace JobApplicationAssistant.Api.Models;

public class SearchCriteria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string TargetJobTitle { get; set; } = string.Empty;
    
    public string? PreferredLocation { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime LastRunAt { get; set; }
}
