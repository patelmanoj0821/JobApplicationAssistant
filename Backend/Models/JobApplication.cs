using System.ComponentModel.DataAnnotations;

namespace JobApplicationAssistant.Api.Models;

public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }
    
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    
    [Required]
    public string JobTitle { get; set; } = string.Empty;
    
    [Required]
    [Url]
    public string JobPostingUrl { get; set; } = string.Empty;
    
    public string? RawJobDescription { get; set; }
    public string? TailoredCoverLetter { get; set; }
    public string? TailoredResumePath { get; set; }
    
    public ApplicationState State { get; set; } = ApplicationState.Discovered;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SubmittedAt { get; set; }
    
    public ICollection<ApplicationLog> Logs { get; set; } = new List<ApplicationLog>();
}
