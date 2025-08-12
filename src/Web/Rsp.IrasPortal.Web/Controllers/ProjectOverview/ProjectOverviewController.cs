using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers.ProjectOverview;

[Route("[controller]/[action]", Name = "pov:[action]")]
[Authorize(Policy = "IsUser")]
public class ProjectOverviewController
(
    IApplicationsService applicationsService,
    IRespondentService respondentService,
    IQuestionSetService questionSetService) : Controller
{
    // ApplicationInfo view name
    private const string ApplicationInfo = nameof(ApplicationInfo);

    public async Task<IActionResult> ProjectDetails(string? projectRecordId, bool preserveProjectOverviewContext = true)
    {
        UpdateModificationRelatedTempData();

        var validationResult = await EnsureProjectOverviewTempData(projectRecordId, preserveProjectOverviewContext);
        if (validationResult is not null)
            return validationResult;

        var model = new ProjectOverviewModel
        {
            ProjectTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecrod,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPlannedEndDate = TempData.Peek(TempDataKeys.PlannedProjectEndDate) as string ?? string.Empty,
            Status = TempData.Peek(TempDataKeys.Status) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId) as int?
        };

        return View(model);
    }

    public async Task<IActionResult> PostApproval(string? projectRecordId, bool preserveProjectOverviewContext = true)
    {
        var validationResult = await EnsureProjectOverviewTempData(projectRecordId, preserveProjectOverviewContext);
        if (validationResult is not null)
            return validationResult;

        var model = new ProjectOverviewModel
        {
            ProjectTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecrod,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPlannedEndDate = TempData.Peek(TempDataKeys.PlannedProjectEndDate) as string ?? string.Empty,
            Status = TempData.Peek(TempDataKeys.Status) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId) as int?
        };

        return View(model);
    }

    private async Task<IActionResult?> EnsureProjectOverviewTempData(string? projectRecordId, bool preserveContext)
    {
        if (projectRecordId is null) { projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty; }
        if (!preserveContext || CheckIfProjectOverviewDataIsMissing())
        {
            return await GetProjectOverview(projectRecordId);
        }

        return null;
    }

    private bool CheckIfProjectOverviewDataIsMissing()
    {
        var irasId = TempData.Peek(TempDataKeys.IrasId) as int?;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId)?.ToString();
        var status = TempData.Peek(TempDataKeys.Status)?.ToString();

        return irasId == null
            || string.IsNullOrWhiteSpace(projectRecordId)
            || string.IsNullOrWhiteSpace(status);
    }

    private void UpdateModificationRelatedTempData()
    {
        // Remove modification-related TempData keys to reset state
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationIdentifier);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationSpecificArea);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges);
        TempData.Remove(TempDataKeys.QuestionSetPublishedVersionId);

        // Indicate that the project overview is being shown
        TempData[TempDataKeys.ProjectOverview] = true;
    }

    /// <summary>
    /// Retrieves and displays the project overview for a given project record.
    /// Fetches the project record and respondent answers, populates TempData with key details.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier for the project record.</param>
    /// <returns>
    /// A <see cref="Task<IActionResult?>"/> that has null value for successful run
    /// or an error details if the project record or answers are not found or a service error occurs.
    /// </returns>
    public async Task<IActionResult?> GetProjectOverview(string projectRecordId)
    {
        // Retrieve the project record by its ID
        var projectRecordResponse = await applicationsService.GetProjectRecord(projectRecordId);

        // If the service call failed, return a generic service error view
        if (!projectRecordResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectRecordResponse);
        }

        var projectRecord = projectRecordResponse.Content;

        if (projectRecord == null)
        {
            // Return a 404 error view if the project record is not found
            return View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "Project record not found",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            });
        }

        // Get all respondent answers for the project and category
        var respondentAnswersResponse = await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecrod);

        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(respondentAnswersResponse);
        }

        var answers = respondentAnswersResponse.Content;

        if (answers == null)
        {
            // Return a 404 error view if no responses are found for the project record
            return View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "No responses found for the project record",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            });
        }

        TempData.TryAdd(TempDataKeys.ProjectRecordResponses, answers, true);

        // Extract key answers from the respondent answers
        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;
        var endDateAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ProjectPlannedEndDate)?.AnswerText;

        // Populate TempData with project details
        TempData[TempDataKeys.IrasId] = projectRecord.IrasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecord.Id;
        TempData[TempDataKeys.Status] = projectRecord.Status;

        if (!string.IsNullOrWhiteSpace(titleAnswer))
        {
            TempData[TempDataKeys.ShortProjectTitle] = titleAnswer;
        }

        var ukCulture = new CultureInfo("en-GB");
        if (DateTime.TryParse(endDateAnswer, ukCulture, DateTimeStyles.None, out var parsedDate))
        {
            TempData[TempDataKeys.PlannedProjectEndDate] = parsedDate.ToString("dd MMMM yyyy");
        }

        return null;
    }
}