using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation;

/// <summary>
/// Controller responsible for handling sponsor workspace related actions.
/// </summary>
[Route("sponsorworkspace/[action]", Name = "sws:[action]")]
[Authorize(Policy = "IsSponsor")]
public class AuthorisationsController
(
    IProjectModificationsService projectModificationsService,
    IValidator<SponsorAuthorisationsSearchModel> searchValidator
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Authorisations
    (
        Guid sponsorOrganisationUserId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ModificationsModel.SentToSponsorDate),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new SponsorAuthorisationsViewModel();

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.SponsorAuthorisationsSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<SponsorAuthorisationsSearchModel>(json)!;
        }

        var searchQuery = new SponsorAuthorisationsSearchRequest()
        {
            SearchTerm = model.Search.SearchTerm,
        };

        // getting modifications by sponsor organisation name
        var projectModificationsServiceResponse = await projectModificationsService.GetModificationsBySponsorOrganisationUserId(sponsorOrganisationUserId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Modifications = projectModificationsServiceResponse?.Content?.Modifications?
                .Select(dto => new ModificationsModel
                {
                    Id = dto.Id,
                    ModificationId = dto.ModificationId,
                    ShortProjectTitle = dto.ShortProjectTitle,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    SponsorOrganisation = dto.SponsorOrganisation,
                    ProjectRecordId = dto.ProjectRecordId,
                    SentToRegulatorDate = dto.SentToRegulatorDate,
                    SentToSponsorDate = dto.SentToSponsorDate,
                    Status = dto.Status
                })
                .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, projectModificationsServiceResponse?.Content?.TotalCount ?? 0)
        {
            RouteName = "sws:authorisations",
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "authorisations-selection",
            AdditionalParameters = new Dictionary<string, string>
            {
                { "SponsorOrganisationUserId", sponsorOrganisationUserId.ToString() }
            }
        };

        model.SponsorOrganisationUserId = sponsorOrganisationUserId;

        return View(model);
    }

    [HttpPost]
    [CmsContentAction(nameof(Authorisations))]
    public async Task<IActionResult> ApplyFilters(SponsorAuthorisationsViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);
            return RedirectToAction(nameof(Authorisations), new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
        }

        HttpContext.Session.SetString(SessionKeys.SponsorAuthorisationsSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Authorisations), new { sponsorOrganisationUserId = model.SponsorOrganisationUserId });
    }
}