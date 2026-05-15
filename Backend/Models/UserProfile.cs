using System.ComponentModel.DataAnnotations;

namespace JobApplicationAssistant.Api.Models;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public string? SerializedResumeData { get; set; }
    
    public string? LinkedInUrl { get; set; }
    
    public string? BaseResumeText { get; set; }
    
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
}
