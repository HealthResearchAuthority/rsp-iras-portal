using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;

/// <summary>
///     Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.Sponsor)]
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
public class AuthorisationsProjectClosuresController
(
    IProjectClosuresService projectClosuresService,
    IUserManagementService userManagementService,
    IValidator<ProjectClosuresSearchModel> searchValidator
) : Controller
{
    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
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

    [Authorize(Policy = Permissions.Sponsor.Modifications_Search)]
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
}