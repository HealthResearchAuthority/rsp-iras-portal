using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = Workspaces.MyResearch)]
public class ApplicationController
(
    IApplicationsService applicationsService,
    IValidator<IrasIdViewModel> irasIdValidator,
    IProjectRecordValidationService projectRecordValidationService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<ApplicationSearchModel> searchValidator,
    IProjectClosuresService projectClosuresService,
    IProjectModificationsService projectModificationsService,
    IValidator<ProjectClosuresModel> closureValidator
) : Controller
{
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_List)]
    public async Task<IActionResult> Welcome
    (
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ApplicationModel.CreatedDate),
        string? sortDirection = SortDirections.Descending
    )
    {
        var model = new ApplicationsViewModel
        {
            EmptySearchPerformed = true, // Set to true to check if search bar should be hidden on view
        };

        // getting respondentID from Http context
        var respondentId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ApplicationSearchModel>(json)!;
            if (model.Search?.Filters?.Count != 0 || !string.IsNullOrEmpty(model.Search.SearchTitleTerm))
            {
                model.EmptySearchPerformed = false;
            }
        }

        var searchQuery = new ApplicationSearchRequest()
        {
            SearchTitleTerm = model.Search.SearchTitleTerm,
            Status = model.Search.Status,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate,
        };

        // getting research applications by respondent ID
        var applicationServiceResponse = await applicationsService.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Applications = applicationServiceResponse.Content?.Items
            .Select(dto => new ApplicationModel
            {
                Id = dto.Id,
                Title = string.IsNullOrWhiteSpace(dto.ShortProjectTitle) ? "Project title" : dto.ShortProjectTitle,
                Status = dto.Status,
                CreatedDate = dto.CreatedDate,
                IrasId = dto.IrasId
            })
            .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, applicationServiceResponse.Content?.TotalCount ?? 0)
        {
            RouteName = "app:welcome",
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "applications-selection"
        };

        return View(nameof(Index), model);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
    public IActionResult StartProject() => View(nameof(StartProject));

    /// <summary>
    /// Handles the POST request to start a new project.
    /// Validates the IRAS ID, checks for duplicates, creates a new application, and redirects to the questionnaire.
    /// </summary>
    /// <param name="model">The IRAS ID view model containing the IRAS ID input by the user.</param>
    /// <returns>
    /// If validation fails or a duplicate IRAS ID is found, returns the StartProject view with errors.
    /// If the application is created successfully, redirects to the Questionnaire Resume action.
    /// Otherwise, returns a service error view.
    /// </returns>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
    [HttpPost]
    [CmsContentAction(nameof(StartProject))]
    public async Task<IActionResult> StartProject(IrasIdViewModel model)
    {
        // Clear session and TempData to ensure a clean state for the new project
        HttpContext.Session.Clear();
        TempData.Clear();

        // Validate the IRAS ID input
        var validationResult = await irasIdValidator.ValidateAsync(new ValidationContext<IrasIdViewModel>(model));

        if (!validationResult.IsValid)
        {
            // Add validation errors to the ModelState
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            // Return the view with validation errors
            return View(nameof(StartProject), model);
        }

        // Retrieve all existing applications
        var applicationsResponse = await applicationsService.GetApplications();
        if (!applicationsResponse.IsSuccessStatusCode || applicationsResponse.Content == null)
        {
            // Return a generic service error view if retrieval fails
            return this.ServiceError(applicationsResponse);
        }

        // Check if an application with the same IRAS ID already exists
        bool irasIdExists = applicationsResponse.Content.Any(app => app.IrasId?.ToString() == model.IrasId);
        if (irasIdExists)
        {
            // Redirect to project already exists page
            return RedirectToRoute("prc:projectrecordexists");
        }

        // validate against the harp database
        var validationServiceResponse = await projectRecordValidationService.ValidateProjectRecord(int.Parse(model.IrasId!));

        if (!validationServiceResponse.IsSuccessStatusCode)
        {
            return validationServiceResponse.StatusCode switch
            {
                // if we don't have a record for the iras id, redirect to not eligible page
                HttpStatusCode.NotFound => RedirectToRoute("prc:projectnoteligible"),
                _ => this.ServiceError(validationServiceResponse),
            };
        }

        // Load the question set for project details
        var questionSetResponse = await cmsQuestionsetService.GetQuestionSet();

        // if we have sections then grab the first section
        var sections = questionSetResponse.Content?.Sections ?? [];

        // section shouldn't be null here, this is a defensive
        // check
        if (sections is { Count: 0 })
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "Unable to load questionnaire for project details",
            });
        }

        // get the first section for the project record questionnaire
        var section = sections[0];

        // confirm project details by playing back short and full project titles
        var projectRecord = validationServiceResponse.Content!;

        TempData[TempDataKeys.ProjectRecord] = JsonSerializer.Serialize(projectRecord.Data);

        // play back project details for confirmation using the ProjectRecord in TempData
        return RedirectToRoute($"prc:{section.StaticViewName}", new
        {
            sectionId = section.Id
        });
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Create)]
    public IActionResult CreateApplication() => View(nameof(CreateApplication));

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Delete)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(string projectRecordId)
    {
        var deleteProjectResponse = await applicationsService.DeleteProject(projectRecordId);

        if (!deleteProjectResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(deleteProjectResponse);
        }

        TempData[TempDataKeys.ShowProjectDeletedBanner] = true;

        return RedirectToRoute("app:welcome");
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        return View();
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Search)]
    [HttpPost]
    [CmsContentAction(nameof(Welcome))]
    public async Task<IActionResult> ApplyFilters(ApplicationsViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(Index), model);
        }

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Welcome));
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Search)]
    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json) ||
            JsonSerializer.Deserialize<ApplicationSearchModel>(json) is not { } search)
        {
            return RedirectToAction(nameof(Welcome));
        }

        var k = key?.ToLowerInvariant().Replace(" ", "");

        void ClearFromDate() => search.FromDay = search.FromMonth = search.FromYear = null;
        void ClearToDate() => search.ToDay = search.ToMonth = search.ToYear = null;

        var actions = new Dictionary<string, Action>(StringComparer.Ordinal)
        {
            ["datecreated"] = () => { ClearFromDate(); ClearToDate(); },
            ["datecreated-from"] = ClearFromDate,
            ["datecreated-to"] = ClearToDate,
            ["status"] = () =>
            {
                if (!string.IsNullOrEmpty(value) && search.Status?.Count > 0)
                {
                    search.Status = search.Status
                        .Where(s => !string.Equals(s, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    search.Status.Clear();
                }
            }
        };

        if (k is not null && actions.TryGetValue(k, out var act))
            act();

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(search));
        return await ApplyFilters(new ApplicationsViewModel { Search = search });
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Search)]
    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json))
            return RedirectToAction(nameof(Welcome));

        if (JsonSerializer.Deserialize<ApplicationSearchModel>(json) is { } search)
        {
            HttpContext.Session.SetString(
                SessionKeys.ProjectRecordSearch,
                JsonSerializer.Serialize(new ApplicationSearchModel { SearchTitleTerm = search.SearchTitleTerm })
            );
        }

        return RedirectToAction(nameof(Welcome));
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Close)]
    [HttpPost]
    public async Task<IActionResult> ConfirmProjectClosure(ProjectClosuresModel model, DateTime plannedProjectEndDate, string separator = "/")
    {
        var status = TempData[TempDataKeys.ProjectRecordStatus];

        if (status is ProjectRecordStatus.PendingClosure)
        {
            return View("/Features/ProjectOverview/Views/ConfirmProjectClosure.cshtml", model);
        }

        var validationResult = await closureValidator.ValidateAsync(model);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            TempData.TryAdd(TempDataKeys.PlannedProjectEndDate, plannedProjectEndDate.ToString("dd MMMM yyyy"));
            return RedirectToAction(nameof(CloseProject), new { projectRecordId = model.ProjectRecordId });
        }

        // Get respondent information from the current context
        var respondent = this.GetRespondentFromContext();

        //// Compose the full name of the respondent
        var userName = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new project modification request
        var projectClosureRequest = new ProjectClosureRequest
        {
            TransactionId = ProjectClosureStatus.TransactionIdPrefix + model.IrasId + separator,
            ProjectRecordId = model.ProjectRecordId,
            IrasId = model.IrasId,
            ClosureDate = model.ActualClosureDate.Date,
            DateActioned = model.DateActioned,
            SentToSponsorDate = DateTime.UtcNow,
            ShortProjectTitle = model.ShortProjectTitle,
            Status = ModificationStatus.WithSponsor,
            CreatedBy = userName,
            UpdatedBy = userName,
        };

        var closeProjectResponse = await projectClosuresService.CreateProjectClosure(projectClosureRequest);

        if (!closeProjectResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(closeProjectResponse);
        }
        //get project record for status update
        var projectRecord = await applicationsService.GetProjectRecord(model.ProjectRecordId);
        if (!projectRecord.IsSuccessStatusCode)
        {
            return this.ServiceError(projectRecord);
        }
        // update the project record status in project record table
        var updateApplicationResponse = await applicationsService.UpdateProjectRecordStatus(projectRecord.Content.Id, ProjectRecordStatus.PendingClosure);

        if (!updateApplicationResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateApplicationResponse);
        }

        TempData[TempDataKeys.ShowCloseProjectBanner] = true;

        return View("/Features/ProjectOverview/Views/ConfirmProjectClosure.cshtml", model);
    }

    /// <summary>
    /// Close project
    /// </summary>
    /// <param name="projectRecordId"></param>
    /// <returns></returns>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Close)]
    [HttpGet]
    public async Task<IActionResult> CloseProject(string projectRecordId)
    {
        var IrasId = TempData[TempDataKeys.IrasId];

        var shortProjectTitle = TempData[TempDataKeys.ShortProjectTitle];

        var modificationsResponse = await projectModificationsService.GetModificationsForProject(projectRecordId, new ModificationSearchRequest());

        var isInTransactionState = modificationsResponse.Content?.Modifications?.Any(m =>
                                m.Status is ModificationStatus.InDraft
                                         or ModificationStatus.WithSponsor
                                         or ModificationStatus.WithReviewBody) == true;

        var model = new ProjectClosuresModel
        {
            ProjectRecordId = projectRecordId,
            IrasId = (int)IrasId,
            ShortProjectTitle = shortProjectTitle.ToString()
        };

        if (isInTransactionState)
        {
            return View("/Features/ProjectOverview/Views/ValidateProjectClosure.cshtml", model);
        }
        else
        {
            var plannedProjectEndDate = HttpContext.Session.GetString(TempDataKeys.PlannedProjectEndDate);
            TempData.TryAdd(TempDataKeys.PlannedProjectEndDate, plannedProjectEndDate);
            return View("/Features/ProjectOverview/Views/CloseProject.cshtml", model);
        }
    }
}