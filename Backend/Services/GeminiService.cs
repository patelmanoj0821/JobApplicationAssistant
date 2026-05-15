using Mscc.GenerativeAI;
using Microsoft.Extensions.Configuration;

namespace JobApplicationAssistant.Api.Services;

public class GeminiService : IAIService
{
    private readonly ILogger<GeminiService> _logger;
    private readonly IConfiguration _config;

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<string> ExtractJobDescriptionAsync(string pageContent, CancellationToken ct = default)
    {
        _logger.LogInformation("Extracting job description using Gemini AI...");
        
        var prompt = "Below is the text content of a job posting page. " +
                     "Please extract only the relevant job description, responsibilities, and requirements. " +
                     "Return only the extracted text, concisely. Content: \n" + pageContent;

        return await ExecuteWithFallback(prompt, ct);
    }

    public async Task<(string CoverLetter, string TailoredResumeTips)> TailorApplicationAsync(string jobDescription, string baseResume, CancellationToken ct = default)
    {
        _logger.LogInformation("Tailoring application using Gemini AI...");

        var prompt = $"Job Description:\n{jobDescription}\n\n" +
                     $"My Base Resume:\n{baseResume}\n\n" +
                     "Based on the above, please generate two things:\n" +
                     "1. A professional, tailored cover letter.\n" +
                     "2. Three specific bullet points to improve my resume for this specific role.\n" +
                     "Separate them with a marker '---RESUME-TIPS---'.";

        var result = await ExecuteWithFallback(prompt, ct);
        
        if (string.IsNullOrEmpty(result) || !result.Contains("---RESUME-TIPS---"))
        {
            result = "Failed to generate tailored content. ---RESUME-TIPS--- No tips generated.";
        }

        var parts = result.Split("---RESUME-TIPS---", StringSplitOptions.RemoveEmptyEntries);
        
        return (
            parts[0].Trim(),
            parts.Length > 1 ? parts[1].Trim() : "No specific tips generated."
        );
    }

    private async Task<string> ExecuteWithFallback(string prompt, CancellationToken ct)
    {
        // Try these model names in order
        string[] modelNames = { "gemini-1.5-flash", "models/gemini-1.5-flash", "gemini-pro", "models/gemini-pro" };
        var errors = new List<string>();

        foreach (var modelName in modelNames)
        {
            try
            {
                _logger.LogInformation("Trying Gemini model: {Model}", modelName);
                var googleAi = new GoogleAI(apiKey: _config["Gemini:ApiKey"] ?? "dummy");
                var model = googleAi.GenerativeModel(model: modelName);
                var response = await model.GenerateContent(prompt);
                if (response != null && !string.IsNullOrEmpty(response.Text))
                {
                    return response.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Model {Model} failed. Error: {Message}", modelName, ex.Message);
                errors.Add($"{modelName}: {ex.Message}");
                continue;
            }
        }

        var errorSummary = string.Join(" | ", errors);
        _logger.LogError("All Gemini models failed. Summary: {Summary}", errorSummary);
        return $"Failed to reach Gemini AI. Errors: {errorSummary}. Please check if 'Generative Language API' is enabled in Google Cloud.";
    }
}
