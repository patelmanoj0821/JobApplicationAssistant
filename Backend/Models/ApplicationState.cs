namespace JobApplicationAssistant.Api.Models;

public enum ApplicationState
{
    Discovered,
    Analyzing,
    Tailoring,
    FillingForm,
    AwaitingManualApproval,
    Submitted,
    Failed
}
