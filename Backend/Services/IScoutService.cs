using JobApplicationAssistant.Api.Models;

namespace JobApplicationAssistant.Api.Services;

public interface IScoutService
{
    Task<List<JobApplication>> ScoutNewJobsAsync(SearchCriteria criteria, CancellationToken ct = default);
}
