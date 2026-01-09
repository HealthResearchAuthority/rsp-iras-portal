using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Controllers;

/// <summary>
///     Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class AuthorisationsProjectClosuresController
(
    IApplicationsService applicationService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IProjectClosuresService projectClosuresService,
    IUserManagementService userManagementService,
    IValidator<ProjectClosuresSearchModel> searchValidator
) : Controller
{
    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Search)]
    [HttpGet]
    public async Task<IActionResult> ProjectClosures
    (
        Guid sponsorOrganisationUserId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectClosuresModel.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new ProjectClosuresViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.SponsorAuthorisationsProjectClosuresSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ProjectClosuresSearchModel>(json)!;
        }

        var searchQuery = new ProjectClosuresSearchRequest
        {
            SearchTerm = model.Search.SearchTerm
        };

        var projectClosuresServiceResponse =
            await projectClosuresService.GetProjectClosuresBySponsorOrganisationUserId(sponsorOrganisationUserId,
                searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.ProjectRecords = projectClosuresServiceResponse?.Content?.ProjectClosures?
            .Select(dto => new ProjectClosuresModel
            {
                Id = dto.Id,
                ProjectRecordId = dto.ProjectRecordId,
                ShortProjectTitle = dto.ShortProjectTitle,
                Status = dto.Status,
                IrasId = dto.IrasId,
                UserId = dto.UserId,
                DateActioned = dto.DateActioned,
                ClosureDate = dto.ClosureDate,
                SentToSponsorDate = dto.SentToSponsorDate
            })
            .ToList() ?? [];

        var userManagementServiceResponse =
            await userManagementService.GetUsersByIds(model.ProjectRecords.Select(r => r.UserId), pageSize: pageSize);

        var emailByUserId = (userManagementServiceResponse?.Content?.Users ?? Enumerable.Empty<User>())
            .ToDictionary(u => u.Id!, u => u.Email);

        foreach (var pr in model.ProjectRecords)
        {
            pr.UserEmail = emailByUserId.TryGetValue(pr.UserId, out var email) ? email : null;
        }

        model.Pagination = new PaginationViewModel(pageNumber, pageSize,
            projectClosuresServiceResponse?.Content?.TotalCount ?? 0)
        {
            RouteName = "sws:projectclosures",
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "projectclosures-selection",
            AdditionalParameters = new Dictionary<string, string>
            {
                { "SponsorOrganisationUserId", sponsorOrganisationUserId.ToString() }
            }
        };

        model.SponsorOrganisationUserId = sponsorOrganisationUserId;

        return View(model);
    }

    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Search)]
    [HttpPost]
    [CmsContentAction(nameof(ProjectClosures))]
    public async Task<IActionResult> ApplyProjectClosuresFilters(ProjectClosuresViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(ProjectClosures),
                new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
        }

        HttpContext.Session.SetString(SessionKeys.SponsorAuthorisationsProjectClosuresSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(ProjectClosures),
            new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
    }

    // 1) Shared builder used by both GET and POST
    [NonAction]
    private async Task<IActionResult> BuildProjectClosuresCheckAndAuthorisePageAsync
    (
        string projectRecordId,
        Guid sponsorOrganisationUserId
    )
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

        // Get questions from CMS service
        var additionalQuestionsResponse = await cmsQuestionsetService.GetQuestionSet();

        // Build the questionnaire model containing all questions for the project ovewrview.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        questionnaire.UpdateWithRespondentAnswers(answers);

        // Extract key answers from the respondent answers
        var endDateAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ProjectPlannedEndDate)?.AnswerText;
        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;

        var projectClosureResponse = await projectClosuresService.GetProjectClosureById(projectRecordId);
        if (!projectClosureResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectClosureResponse);
        }

        var actualEndDate =
            projectClosureResponse.Content?.ClosureDate is DateTime dt
                ? DateHelper.ConvertDateToString(dt)
                : null;

        var model = new AuthoriseProjectClosuresOutcomeViewModel
        {
            ProjectRecordId = projectRecordId,
            SponsorOrganisationUserId = sponsorOrganisationUserId,
            ActualEndDate = DateHelper.ConvertDateToString(actualEndDate),
            PlannedEndDate = DateHelper.ConvertDateToString(endDateAnswer),
            IrasId = projectRecord.IrasId,
            ShortProjectTitle = titleAnswer,
        };

        return Ok(model);
    }

    // 2) GET stays tiny and calls the builder
    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Review)]
    [HttpGet]
    public async Task<IActionResult> CheckAndAuthoriseProjectClosure(string projectRecordId, Guid sponsorOrganisationUserId)
    {
        var result =
            await BuildProjectClosuresCheckAndAuthorisePageAsync(projectRecordId, sponsorOrganisationUserId);

        if (result is not OkObjectResult outcome)
        {
            return result;
        }

        return View(outcome.Value);
    }

    // 3) POST: on invalid, rebuild the page VM and return the same view with ModelState errors
    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Authorise)]
    [HttpPost]
    public async Task<IActionResult> CheckAndAuthoriseProjectClosure(AuthoriseProjectClosuresOutcomeViewModel model)
    {
        // 🟢 Always build the page first, so it's hydrated for both success and error paths
        var result = await BuildProjectClosuresCheckAndAuthorisePageAsync(
            model.ProjectRecordId,
            model.SponsorOrganisationUserId
        );

        if (result is not OkObjectResult res)
        {
            return result;
        }

        if (!ModelState.IsValid)
        {
            var hydrated = res.Value as AuthoriseProjectClosuresOutcomeViewModel;
            // Preserve the posted Outcome so the radios keep the selection
            if (hydrated is not null)
            {
                hydrated.Outcome = model.Outcome;
                // copy any other posted fields you want to preserve on re-render
            }

            return View(hydrated);
        }

        if (model.Outcome == nameof(ProjectClosureStatus.Authorised))
        {
            TempData[TempDataKeys.PreAuthProjectClosureModel] = JsonSerializer.Serialize(model);
            return RedirectToAction(nameof(ProjectClosurePreAuthorisation));
        }
        else if (model.Outcome == nameof(ProjectClosureStatus.NotAuthorised))
        {
            await projectClosuresService.UpdateProjectClosureStatus
            (
                model.ProjectRecordId,
                ProjectClosureStatus.NotAuthorised
            );
            return RedirectToAction(nameof(ProjectClosureConfirmation), model);
        }
        else
        {
            var serviceResponse = new ServiceResponse()
                .WithError("Project closure status has not been selected")
                .WithStatus(HttpStatusCode.BadRequest);
            return this.ServiceError(serviceResponse);
        }
    }

    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Authorise)]
    [HttpGet]
    public IActionResult ProjectClosurePreAuthorisation()
    {
        var json = TempData.Peek(TempDataKeys.PreAuthProjectClosureModel) as string;
        if (!string.IsNullOrEmpty(json))
        {
            var model = JsonSerializer.Deserialize<AuthoriseProjectClosuresOutcomeViewModel>(json);
            return View(model);
        }
        else
        {
            var serviceResponse = new ServiceResponse()
                .WithError("Missing tempdata for project closure authorisation")
                .WithStatus(HttpStatusCode.BadRequest);
            return this.ServiceError(serviceResponse);
        }
    }

    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Authorise)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProjectClosurePreAuthorisationConfirm()
    {
        if (TempData.TryGetValue(TempDataKeys.PreAuthProjectClosureModel, out var json) && json is string serialized)
        {
            var model = JsonSerializer.Deserialize<AuthoriseProjectClosuresOutcomeViewModel>(serialized);
            await projectClosuresService.UpdateProjectClosureStatus
            (
                model.ProjectRecordId,
                ProjectClosureStatus.Authorised
            );
            return RedirectToAction(nameof(ProjectClosureConfirmation), model);
        }
        else
        {
            var serviceResponse = new ServiceResponse()
                .WithError("Missing tempdata for project closure authorisation")
                .WithStatus(HttpStatusCode.BadRequest);
            return this.ServiceError(serviceResponse);
        }
    }

    [Authorize(Policy = Permissions.Sponsor.ProjectClosures_Review)]
    [HttpGet]
    public IActionResult ProjectClosureConfirmation(AuthoriseProjectClosuresOutcomeViewModel model)
    {
        return View(model);
    }
}