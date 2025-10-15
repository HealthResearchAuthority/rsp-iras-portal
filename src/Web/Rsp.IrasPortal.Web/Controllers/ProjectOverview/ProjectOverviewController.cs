using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Enum;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers.ProjectOverview;

[Route("[controller]/[action]", Name = "pov:[action]")]
[Authorize(Policy = "IsBackstageUser")]
public class ProjectOverviewController(
    IApplicationsService applicationService,
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService) : Controller
{
    private const string DocumentDetailsSection = "pdm-document-metadata";

    [Route("/projectoverview", Name = "pov:index")]
    public async Task<IActionResult> Index(string projectRecordId, string? backRoute, string? modificationId)
    {
        var response = await GetProjectOverview(projectRecordId);
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);

        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        if (okResult.Value is ProjectOverviewModel model && model.Status == ProjectRecordStatus.Active)
        {
            return RedirectToAction(nameof(ProjectDetails), new { projectRecordId, backRoute, modificationId });
        }
        return View(okResult.Value);
    }

    public async Task<IActionResult> ProjectDetails(string projectRecordId, string? backRoute, string? modificationId)
    {
        // IF NAVIGATED FROM SHORT PROJECT TITLE LINKS
        if (backRoute != null)
        {
            TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectRecordId;
        }

        UpdateModificationRelatedTempData();
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);

        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        return View(okResult.Value);
    }

    public async Task<IActionResult> PostApproval
    (
        string projectRecordId,
        string? backRoute,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.CreatedAt),
        string sortDirection = SortDirections.Descending
    )
    {
        UpdateModificationRelatedTempData();
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);

        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        var model = new PostApprovalViewModel
        {
            ProjectOverviewModel = okResult.Value as ProjectOverviewModel
        };

        var searchQuery = new ModificationSearchRequest();

        var modificationsResponseResult =
            await projectModificationsService.GetModificationsForProject(projectRecordId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Modifications = modificationsResponseResult?.Content?.Modifications?
            .Select(dto => new PostApprovalModificationsModel
            {
                ModificationId = dto.Id,
                ModificationIdentifier = dto.ModificationId,
                ModificationType = dto.ModificationType,
                ReviewType = null,
                Category = null,
                DateSubmitted = dto.SubmittedDate,
                Status = dto.Status,
            })
            .OrderBy(item => Enum.TryParse<ModificationStatusOrder>(item.Status, true, out var statusEnum)
            ? (int)statusEnum
            : (int)ModificationStatusOrder.None)
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

    public async Task<IActionResult> ProjectTeam(string projectRecordId, string? backRoute)
    {
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);
        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        return View(okResult.Value);
    }

    public async Task<IActionResult> ResearchLocations(string projectRecordId, string? backRoute)
    {
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);
        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        return View(okResult.Value);
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
    [NonAction]
    public async Task<IActionResult> GetProjectOverview(string projectRecordId)
    {
        // Retrieve the project record by its ID
        var projectRecordResponse = await applicationService.GetProjectRecord(projectRecordId);

        // If the service call failed, return a generic service error view
        if (!projectRecordResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectRecordResponse);
        }

        var projectRecord = projectRecordResponse.Content;

        if (projectRecord == null)
        {
            // Return a 404 error view if the project record is not found
            var serviceResponse = new ServiceResponse()
                .WithError("Project record not found")
                .WithStatus(HttpStatusCode.NotFound);

            return this.ServiceError(serviceResponse);
        }

        // Get all respondent answers for the project and category
        var respondentAnswersResponse =
            await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecrod);

        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(respondentAnswersResponse);
        }

        var answers = respondentAnswersResponse.Content;

        if (answers == null)
        {
            // Return a 404 error view if no responses are found for the project record
            var serviceResponse = new ServiceResponse()
                .WithError("No responses found for the project record")
                .WithStatus(HttpStatusCode.NotFound);

            return this.ServiceError(serviceResponse);
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
                .ConvertAll(id => answerOptions.TryGetValue(id, out var name) ? name : id)
            ;

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

        return Ok(model);
    }

    public async Task<IActionResult> ProjectDocuments
        (
        string projectRecordId,
        string? backRoute,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending
        )
    {
        UpdateModificationRelatedTempData();
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);

        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        var model = new ProjectOverviewDocumentViewModel
        {
            ProjectOverviewModel = okResult.Value as ProjectOverviewModel
        };

        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the questionnaire model containing all questions for the details section.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        var matchingQuestion = questionnaire.Questions.FirstOrDefault(q => q.QuestionId == ModificationQuestionIds.DocumentType);

        searchQuery.DocumentTypes = matchingQuestion?.Answers?
            .ToDictionary(a => a.AnswerId, a => a.AnswerText) ?? [];

        var modificationsResponseResult = await projectModificationsService.GetDocumentsForProjectOverview(projectRecordId,
            searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Documents = modificationsResponseResult?.Content?.Documents ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationsResponseResult?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "projectdocuments-selection",
            RouteName = "pov:projectdocuments",
            AdditionalParameters = new Dictionary<string, string>() { { "projectRecordId", projectRecordId } }
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmDeleteProject(string projectRecordId)
    {
        var response = await GetProjectOverview(projectRecordId);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            (response is not OkObjectResult okResult))
        {
            return response;
        }

        var model = okResult.Value as ProjectOverviewModel;

        return View("/Features/ProjectOverview/Views/DeleteProject.cshtml", model);
    }

    private static string? GetAnswerName(string? answerText, Dictionary<string, string> options)
    {
        return answerText is string id && options.TryGetValue(id, out var name) ? name : null;
    }

    private void UpdateModificationRelatedTempData()
    {
        // If there is a project modification change, show the notification banner
        if
        (
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is not null &&
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] is Guid marker && marker != Guid.Empty
        )
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;
        }

        // Remove modification-related TempData keys to reset state
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationIdentifier);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeText);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeMarker);
        TempData.Remove(TempDataKeys.ProjectModification.LinkBackToReferrer);
        TempData.Remove(TempDataKeys.ProjectModification.UrlReferrer);

        var keys = TempData.Keys.Where(key => key.StartsWith(TempDataKeys.ProjectModification.Questionnaire));

        foreach (var key in keys)
        {
            TempData.Remove(key);
        }

        // Indicate that the project overview is being shown
        TempData[TempDataKeys.ProjectOverview] = true;
    }

    private void SetupShortProjectTitleBackNav(string sectionId, string defaultRoute = "app:Welcome",
        string? backRouteFromQuery = null)
    {
        // If a backRoute is supplied on the query, store it (and any brv_* values) for this section
        if (!string.IsNullOrWhiteSpace(backRouteFromQuery))
        {
            HttpContext.Session.SetString(SessionKeys.BackRoute, backRouteFromQuery);
            HttpContext.Session.SetString(SessionKeys.BackRouteSection, sectionId);

            ViewData["BackRoute"] = backRouteFromQuery;
            return;
        }

        // No backRoute in the query: try session, but only if we're still in the same section
        var storedSection = HttpContext.Session.GetString(SessionKeys.BackRouteSection);
        if (string.Equals(storedSection, sectionId, StringComparison.OrdinalIgnoreCase))
        {
            var storedRoute = HttpContext.Session.GetString(SessionKeys.BackRoute);
            ViewData["BackRoute"] = storedRoute ?? defaultRoute;
        }
        else
        {
            // Different section: clear old and fall back to default
            HttpContext.Session.Remove(SessionKeys.BackRoute);
            HttpContext.Session.Remove(SessionKeys.BackRouteSection);

            ViewData["BackRoute"] = defaultRoute;
        }
    }
}