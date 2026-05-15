using JobApplicationAssistant.Api.Models;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using System.Text.Json;

namespace JobApplicationAssistant.Api.Services;

public class UniversalScoutService : IScoutService
{
    private readonly ILogger<UniversalScoutService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public UniversalScoutService(IConfiguration config, ILogger<UniversalScoutService> logger, HttpClient httpClient)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<JobApplication>> ScoutNewJobsAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        // 1. Try Google Custom Search API first
        var googleJobs = await TryGoogleSearchAsync(criteria, ct);
        if (googleJobs != null && googleJobs.Any())
        {
            return googleJobs;
        }

        // 2. If Google fails (403/Quota), fallback to SerpApi
        _logger.LogWarning("Google API failed or returned no results. Falling back to SerpApi...");
        return await TrySerpApiSearchAsync(criteria, ct);
    }

    private async Task<List<JobApplication>?> TryGoogleSearchAsync(SearchCriteria criteria, CancellationToken ct)
    {
        var apiKey = _config["GoogleSearch:ApiKey"];
        var cx = _config["GoogleSearch:SearchEngineId"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(cx)) return null;

        try
        {
            var service = new CustomSearchAPIService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = "JobApplicationAssistant"
            });

            var request = service.Cse.List();
            request.Cx = cx;
            request.Q = $"{criteria.TargetJobTitle} in United States site:boards.greenhouse.io OR site:lever.co OR site:linkedin.com/jobs";

            var result = await request.ExecuteAsync(ct);
            var jobs = new List<JobApplication>();
            
            if (result.Items != null)
            {
                foreach (var item in result.Items)
                {
                    jobs.Add(new JobApplication
                    {
                        CompanyName = ExtractCompanyName(item.Title),
                        JobTitle = item.Title,
                        JobPostingUrl = item.Link,
                        State = ApplicationState.Discovered
                    });
                }
            }
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError("Google Search API Error: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<List<JobApplication>> TrySerpApiSearchAsync(SearchCriteria criteria, CancellationToken ct)
    {
        var apiKey = _config["SerpApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("SerpApi Key is also missing. Cannot scout.");
            return new List<JobApplication>();
        }

        try
        {
            var query = $"{criteria.TargetJobTitle} in United States site:boards.greenhouse.io OR site:lever.co OR site:linkedin.com/jobs";
            var url = $"https://serpapi.com/search.json?q={Uri.EscapeDataString(query)}&api_key={apiKey}&engine=google&gl=us&hl=en";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return new List<JobApplication>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            var jobs = new List<JobApplication>();
            if (doc.RootElement.TryGetProperty("organic_results", out var results))
            {
                foreach (var item in results.EnumerateArray().Take(5))
                {
                    var title = item.GetProperty("title").GetString() ?? "Discovered Job";
                    var link = item.GetProperty("link").GetString() ?? "";
                    jobs.Add(new JobApplication
                    {
                        CompanyName = ExtractCompanyName(title),
                        JobTitle = title,
                        JobPostingUrl = link,
                        State = ApplicationState.Discovered
                    });
                }
            }
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError("SerpApi Error: {Message}", ex.Message);
            return new List<JobApplication>();
        }
    }

    private string ExtractCompanyName(string title)
    {
        if (title.Contains(" - ")) return title.Split(" - ").Last().Trim();
        if (title.Contains(" at ")) return title.Split(" at ").Last().Split("-").First().Trim();
        return "Discovered Opportunity";
    }
}
