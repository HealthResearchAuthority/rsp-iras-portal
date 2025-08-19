using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers.ProjectOverview;

[Route("[controller]/[action]", Name = "pov:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ProjectOverviewController
(
    IApplicationsService applicationService,
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService) : Controller
{
    public async Task<IActionResult> ProjectDetails(string projectRecordId)
    {
        UpdateModificationRelatedTempData();

        var response = await GetProjectOverview(projectRecordId);
        if (response.Error is not null)
        {
            return response.Error;
        }
        return View(response.Model);
    }

    public async Task<IActionResult> PostApproval
    (
        string projectRecordId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.CreatedAt),
        string sortDirection = SortDirections.Ascending
    )
    {
        UpdateModificationRelatedTempData();

        var response = await GetProjectOverview(projectRecordId);
        if (response.Error is not null)
        {
            return response.Error;
        }

        var model = new PostApprovalViewModel();
        model.ProjectOverviewModel = response.Model;

        ModificationSearchRequest searchQuery = new ModificationSearchRequest();

        var modificationsResponseResult = await projectModificationsService.GetModificationsForProject(projectRecordId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Modifications = modificationsResponseResult?.Content?.Modifications?
                    .Select(dto => new PostApprovalModificationsModel
                    {
                        ModificationId = dto.ModificationId,
                        ModificationType = dto.ModificationType,
                        ReviewType = null,
                        Category = null,
                        DateSubmitted = null,
                        Status = "Draft"
                    })
                    .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationsResponseResult?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "postapproval-selection",
            RouteName = "pov:postapproval",
            AdditionalParameters = new Dictionary<string, string>() { { "projectRecordId", projectRecordId } }
        };

        return View(model);
    }

    public async Task<IActionResult> KeyProjectRoles(string projectRecordId)
    {
        var response = await GetProjectOverview(projectRecordId);
        if (response.Error is not null)
        {
            return response.Error;
        }
        return View(response.Model);
    }

    public async Task<IActionResult> ResearchLocations(string projectRecordId)
    {
        var response = await GetProjectOverview(projectRecordId);
        if (response.Error is not null)
        {
            return response.Error;
        }
        return View(response.Model);
    }

    private void UpdateModificationRelatedTempData()
    {
        // If there is a project modification change, show the notification banner
        if (TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is not null &&
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] is Guid marker &&
            marker != Guid.Empty)
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;
        }

        // Remove modification-related TempData keys to reset state
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationIdentifier);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationSpecificArea);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeMarker);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges);
        TempData.Remove(TempDataKeys.QuestionSetPublishedVersionId);

        var keys = TempData.Keys.Where(key => key.StartsWith(TempDataKeys.ProjectModification.Questionnaire));

        foreach (var key in keys)
        {
            TempData.Remove(key);
        }

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
    public async Task<(IActionResult Error, ProjectOverviewModel? Model)> GetProjectOverview(string projectRecordId)
    {
        // Retrieve the project record by its ID
        var projectRecordResponse = await applicationService.GetProjectRecord(projectRecordId);

        // If the service call failed, return a generic service error view
        if (!projectRecordResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(projectRecordResponse), null);
        }

        var projectRecord = projectRecordResponse.Content;

        if (projectRecord == null)
        {
            // Return a 404 error view if the project record is not found
            return (View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "Project record not found",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            }), null);
        }

        // Get all respondent answers for the project and category
        var respondentAnswersResponse = await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecrod);

        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(respondentAnswersResponse), null);
        }

        var answers = respondentAnswersResponse.Content;

        if (answers == null)
        {
            // Return a 404 error view if no responses are found for the project record
            return (View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "No responses found for the project record",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            }), null);
        }

        TempData.TryAdd(TempDataKeys.ProjectRecordResponses, answers, true);

        Dictionary<string, string> answerOptions = new()
        {
            { QuestionAnswersOptionsIds.Yes, "Yes" },
            { QuestionAnswersOptionsIds.No, "No" },
            { QuestionAnswersOptionsIds.England, "England" },
            { QuestionAnswersOptionsIds.NorthernIreland, "Northern Ireland" },
            { QuestionAnswersOptionsIds.Scotland, "Scotland" },
            { QuestionAnswersOptionsIds.Wales, "Wales" }
        };

        // Extract key answers from the respondent answers
        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;
        var endDateAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ProjectPlannedEndDate)?.AnswerText;

        var participatingNations = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ParticipatingNations)?
            .Answers
            .Select(id => answerOptions.TryGetValue(id, out var name) ? name : id)
            .ToList();

        var nhsOrHscOrganisations = GetAnswerName(answers.FirstOrDefault(a => a.QuestionId == QuestionIds.NhsOrHscOrganisations)?.SelectedOption, answerOptions);
        var LeadNation = GetAnswerName(answers.FirstOrDefault(a => a.QuestionId == QuestionIds.LeadNation)?.SelectedOption, answerOptions);

        var chiefInvestigator = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ChiefInvestigator)?.AnswerText;
        var primarySponsorOrganisation = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.PrimarySponsorOrganisation)?.AnswerText;
        var sponsorContact = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.SponsorContact)?.AnswerText;

        // Populate TempData with project details for actual modification journey
        TempData[TempDataKeys.IrasId] = projectRecord.IrasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecord.Id;
        TempData[TempDataKeys.ShortProjectTitle] = titleAnswer as string ?? string.Empty;

        var ukCulture = new CultureInfo("en-GB");
        string? projectPlannedEndDate = null;
        if (DateTime.TryParse(endDateAnswer, ukCulture, DateTimeStyles.None, out var parsedDate))
        {
            projectPlannedEndDate = parsedDate.ToString("dd MMMM yyyy");
            TempData[TempDataKeys.PlannedProjectEndDate] = projectPlannedEndDate;
        }

        var model = new ProjectOverviewModel
        {
            ProjectTitle = titleAnswer as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecrod,
            ProjectRecordId = projectRecord.Id,
            ProjectPlannedEndDate = projectPlannedEndDate,
            Status = projectRecord.Status,
            IrasId = projectRecord.IrasId,
            ParticipatingNations = participatingNations,
            NhsOrHscOrganisations = nhsOrHscOrganisations,
            LeadNation = LeadNation,
            ChiefInvestigator = chiefInvestigator,
            PrimarySponsorOrganisation = primarySponsorOrganisation,
            SponsorContact = sponsorContact
        };

        return (null, model);
    }

    private string? GetAnswerName(string? answerText, Dictionary<string, string> options)
    {
        return answerText is string id && options.TryGetValue(id, out var name) ? name : null;
    }
}