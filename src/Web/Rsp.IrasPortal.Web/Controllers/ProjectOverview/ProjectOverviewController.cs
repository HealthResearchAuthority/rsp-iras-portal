using System.Net;
using System.Text.Json;
using FluentValidation;
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
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers.ProjectOverview;

[Route("[controller]/[action]", Name = "pov:[action]")]
[Authorize(Policy = "IsBackstageUser")]
public class ProjectOverviewController(
    IApplicationsService applicationService,
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRtsService rtsService,
    IValidator<ApprovalsSearchModel> validator,
    IValidator<QuestionnaireViewModel> docValidator
    ) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, docValidator)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";

    [Route("/projectoverview", Name = "pov:index")]
    public async Task<IActionResult> Index(string projectRecordId, string? backRoute, string? modificationId)
    {
        var result = await GetProjectOverviewResult(projectRecordId!, backRoute);

        if (result is not OkObjectResult projectOverview)
        {
            return result;
        }

        if (projectOverview.Value is ProjectOverviewModel model && model.Status == ProjectRecordStatus.Active)
        {
            return RedirectToAction(nameof(ProjectDetails), new { projectRecordId, backRoute, modificationId });
        }
        return View(projectOverview.Value);
    }

    public async Task<IActionResult> ProjectDetails(string projectRecordId, string? backRoute, string? modificationId)
    {
        // IF NAVIGATED FROM SHORT PROJECT TITLE LINKS
        if (backRoute != null)
        {
            TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectRecordId;
        }

        UpdateModificationRelatedTempData();

        var result = await GetProjectOverviewResult(projectRecordId!, backRoute, nameof(ProjectDetails));

        if (result is not OkObjectResult projectOverview)
        {
            return result;
        }

        return View(projectOverview.Value);
    }

    public async Task<IActionResult> PostApproval
    (
        string projectRecordId,
        string? backRoute,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = null,
        string? sortDirection = null
    )
    {
        UpdateModificationRelatedTempData();

        var result = await GetProjectOverviewResult(projectRecordId!, backRoute);

        if (result is not OkObjectResult projectOverview)
        {
            return result;
        }

        var model = new PostApprovalViewModel
        {
            ProjectOverviewModel = projectOverview.Value as ProjectOverviewModel
        };
        if (HttpContext.Request.Method == HttpMethods.Get)
        {
            var savedSearch = HttpContext.Session.GetString(SessionKeys.PostApprovalsSearch);
            if (!string.IsNullOrWhiteSpace(savedSearch))
            {
                model.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(savedSearch)!;
            }
        }

        var searchQuery = new ModificationSearchRequest()
        {
            FromDate = model.Search?.FromDate,
            ToDate = model.Search?.ToDate,
            ModificationType = model.Search?.ModificationType!,
            Category = model.Search?.Category,
            ReviewType = model.Search?.ReviewType,
            Status = model.Search?.Status,
            ModificationId = model.Search?.ModificationId,
        };

        var modificationsResponseResult =
            await projectModificationsService.GetModificationsForProject(projectRecordId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Modifications = modificationsResponseResult?.Content?.Modifications?
            .Select(dto => new PostApprovalModificationsModel
            {
                ModificationId = dto.Id,
                ModificationIdentifier = dto.ModificationId,
                ModificationType = dto.ModificationType,
                ReviewType = dto.ReviewType,
                Category = dto.Category,
                SentToSponsorDate = dto.SentToSponsorDate,
                SentToRegulatorDate = dto.SentToRegulatorDate,
                Status = dto.Status,
            })
            .ToList() ?? [];
        if (string.IsNullOrEmpty(sortField) && string.IsNullOrEmpty(sortDirection))
        {
            model.Modifications = model.Modifications.OrderBy(item => Enum.TryParse<ModificationStatusOrder>(GetEnumStatus(item.Status!), true, out var statusEnum)
                    ? (int)statusEnum
                    : (int)ModificationStatusOrder.None)
                .ToList() ?? [];
        }
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
        var result = await GetProjectOverviewResult(projectRecordId!, backRoute, nameof(ProjectTeam));
        if (result is not OkObjectResult okResult)
        {
            return result;
        }

        return View(okResult.Value);
    }

    public async Task<IActionResult> ResearchLocations(string projectRecordId, string? backRoute)
    {
        var result = await GetProjectOverviewResult(projectRecordId!, backRoute, nameof(ResearchLocations));
        if (result is not OkObjectResult okResult)
        {
            return result;
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
    public async Task<IActionResult> GetProjectOverview(string projectRecordId, string? specificViewName = null)
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
            await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecord);

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

        // Get questions from CMS service
        var additionalQuestionsResponse = await cmsQuestionsetService.GetQuestionSet();

        // Build the questionnaire model containing all questions for the project ovewrview.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        questionnaire.UpdateWithRespondentAnswers(answers);

        // Extract key answers from the respondent answers
        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;
        var endDateAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ProjectPlannedEndDate)?.AnswerText;

        // Get list of questions for specific project overview tab.
        var sectionGroupQuestions = new List<SectionGroupWithQuestionsViewModel>();

        if (specificViewName is not null)
        {
            sectionGroupQuestions = questionnaire.Questions
                .Where(q => q.ShowAnswerOn.Contains(specificViewName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(q => q.SectionGroup)
                .Select(g => new SectionGroupWithQuestionsViewModel
                {
                    SectionGroup = g.Key!,
                    SectionSequence = g.First().SectionSequence,
                    Questions = g.OrderBy(q => q.SequenceInSectionGroup).ToList()
                })
                .OrderBy(g => g.SectionSequence) // Using SectionSequence as there is no separate parameter for group order
                .ToList();
        }

        // Get organisation name from RTS service
        var organisationName = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsService, questionnaire.Questions);

        var auditTrails = await applicationService.GetProjectRecordAuditTrail(projectRecordId);

        // Populate TempData with project details for actual modification journey
        TempData[TempDataKeys.IrasId] = projectRecord.IrasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecord.Id;
        TempData[TempDataKeys.ShortProjectTitle] = titleAnswer as string ?? string.Empty;
        TempData[TempDataKeys.PlannedProjectEndDate] = DateHelper.ConvertDateToString(endDateAnswer);

        var model = new ProjectOverviewModel
        {
            ProjectTitle = titleAnswer as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecord,
            ProjectRecordId = projectRecord.Id,
            ProjectPlannedEndDate = DateHelper.ConvertDateToString(endDateAnswer),
            Status = projectRecord.Status,
            IrasId = projectRecord.IrasId,
            OrganisationName = organisationName,
            SectionGroupQuestions = sectionGroupQuestions,
            AuditTrails = auditTrails.Content?.Items ?? []
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

        var result = await GetProjectOverviewResult(projectRecordId!, backRoute);

        if (result is not OkObjectResult okResult)
        {
            return result;
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

        await MapDocumentTypesAndStatusesAsync(questionnaire, model.Documents);

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

    public async Task<IActionResult> ProjectHistory(string projectRecordId, string? backRoute)
    {
        UpdateModificationRelatedTempData();

        var result = await GetProjectOverviewResult(projectRecordId!, backRoute, nameof(ProjectHistory));

        if (result is not OkObjectResult projectOverview)
        {
            return result;
        }

        return View(projectOverview.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmDeleteProject(string projectRecordId)
    {
        var result = await GetProjectOverviewResult(projectRecordId!, "");

        if (result is not OkObjectResult okResult)
        {
            return result;
        }

        var model = okResult.Value as ProjectOverviewModel;

        return View("/Features/ProjectOverview/Views/DeleteProject.cshtml", model);
    }

    [Route("/projectoverview/applyfilters", Name = "pov:applyfilters")]
    [HttpPost]
    public async Task<IActionResult> ApplyFilters(PostApprovalViewModel model)
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;

        var result = await GetProjectOverviewResult(projectRecordId!, "");

        if (result is not OkObjectResult projectOverview)
        {
            return result;
        }

        model.ProjectOverviewModel = projectOverview.Value as ProjectOverviewModel;

        var validationResult = await validator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(PostApproval), model);
        }

        HttpContext.Session.SetString(SessionKeys.PostApprovalsSearch, JsonSerializer.Serialize(model.Search));

        // Call PostApproval with matching parameter set
        return RedirectToRoute("pov:postapproval", new { projectRecordId });
    }

    [Route("/projectoverview/clearfilters", Name = "pov:clearfilters")]
    [HttpGet]
    public IActionResult ClearFilters()
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId);
        HttpContext.Session.Remove(SessionKeys.PostApprovalsSearch);
        return RedirectToRoute("pov:postapproval", new { projectRecordId });
    }

    [Route("/projectoverview/removefilter", Name = "pov:removefilter")]
    [HttpGet]
    public IActionResult RemoveFilter(string key, [FromQuery] string? model = null)
    {
        var viewModel = new PostApprovalViewModel();

        if (!string.IsNullOrWhiteSpace(model))
        {
            viewModel.Search = JsonSerializer.Deserialize<ApprovalsSearchModel>(model);
        }
        else
        {
            viewModel.Search = new ApprovalsSearchModel();
        }
        var keyNormalized = key?.ToLowerInvariant().Replace(" ", "");

        switch (keyNormalized)
        {
            case "modificationtype":
                viewModel.Search!.ModificationType = null;
                break;

            case "reviewtype":
                viewModel.Search!.ReviewType = null;
                break;

            case "category":
                viewModel.Search!.Category = null;
                break;

            case "datesubmitted":
                viewModel.Search!.FromDay = viewModel.Search.FromMonth = viewModel.Search.FromYear = null;
                viewModel.Search.ToDay = viewModel.Search.ToMonth = viewModel.Search.ToYear = null;
                break;

            case "datesubmitted-from":
                viewModel.Search!.FromDay = viewModel.Search.FromMonth = viewModel.Search.FromYear = null;
                break;

            case "datesubmitted-to":
                viewModel.Search!.ToDay = viewModel.Search.ToMonth = viewModel.Search.ToYear = null;
                break;

            case "status":
                viewModel.Search!.Status = null;
                break;
        }

        HttpContext.Session.SetString(SessionKeys.PostApprovalsSearch, JsonSerializer.Serialize(viewModel.Search));

        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId);

        return RedirectToRoute("pov:postapproval", new { projectRecordId });
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

    private static string? GetEnumStatus(string status) => status switch
    {
        ModificationStatus.InDraft => nameof(ModificationStatusOrder.InDraft),
        ModificationStatus.WithSponsor => nameof(ModificationStatusOrder.WithSponsor),
        ModificationStatus.WithReviewBody => nameof(ModificationStatusOrder.WithRegulator),
        ModificationStatus.Approved => nameof(ModificationStatusOrder.Approved),
        ModificationStatus.NotApproved => nameof(ModificationStatusOrder.NotApproved),
        ModificationStatus.NotAuthorised => nameof(ModificationStatusOrder.NotAuthorised),
        _ => ModificationStatusOrder.None.ToString()
    };

    private async Task<IActionResult> GetProjectOverviewResult(string projectRecordId, string? backRoute, string? specificViewName = null)
    {
        SetupShortProjectTitleBackNav("pov", "app:Welcome", backRoute);

        var response = await GetProjectOverview(projectRecordId, specificViewName);

        // if status code is not a successful status code
        if ((response is StatusCodeResult result && result.StatusCode is < 200 or > 299) ||
            response is not OkObjectResult)
        {
            return response;
        }

        return response;
    }
}