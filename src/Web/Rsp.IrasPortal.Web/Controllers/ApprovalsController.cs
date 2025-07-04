using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Policy = "IsUser")]
public class ApprovalsController(
    IRtsService rtsService,
    IValidator<ApprovalsSearchModel> validator) : Controller
{
    public IActionResult Welcome()
    {
        return View(nameof(Index));
    }

    [HttpGet]
    public IActionResult Search()
    {
        ApprovalsSearchModel model = new();

        if (HttpContext.Session.Keys.Contains(SessionKeys.ApprovalsSearch))
        {
            var json = HttpContext.Session.GetString(SessionKeys.ApprovalsSearch)!;
            model = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;
        }

        return View(model);
    }

    [HttpPost]
    [Route("/approvals/applyfilters", Name = "approvals:applyfilters")]
    public async Task<IActionResult> ApplyFilters(ApprovalsSearchModel model)
    {
        var validationResult = await validator.ValidateAsync(model);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                model.Filters = new Dictionary<string, string>();
            }

            return View("Search", model);
        }

        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(model));
        return View("Search", model);
    }

    [HttpGet]
    [Route("/approvals/clearfilters", Name = "approvals:clearfilters")]
    public IActionResult ClearFilters()
    {
        var model = new ApprovalsSearchModel();
        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(model));
        return View("Search", model);
    }

    [HttpGet]
    [Route("/approvals/removefilter", Name = "approvals:removefilter")]
    public async Task<IActionResult> RemoveFilter(string key, string? value)
    {
        ApprovalsSearchModel model = new();

        if (HttpContext.Session.Keys.Contains(SessionKeys.ApprovalsSearch))
        {
            var json = HttpContext.Session.GetString(SessionKeys.ApprovalsSearch)!;
            model = JsonSerializer.Deserialize<ApprovalsSearchModel>(json)!;

            if (model.Filters.TryGetValue(key, out var existingValue))
            {
                var updatedValues = existingValue
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => !string.Equals(v, value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (updatedValues.Any())
                {
                    model.Filters[key] = string.Join(", ", updatedValues);
                }
                else
                {
                    model.Filters.Remove(key);
                }
            }
        }

        switch (key.ToLowerInvariant().Replace(" ", ""))
        {
            case "irasid":
                model.IrasId = null;
                break;

            case "chiefinvestigatorname":
                model.ChiefInvestigatorName = null;
                break;

            case "projecttitle":
                model.ShortProjectTitle = null;
                break;

            case "sponsororganisation":
                model.SponsorOrganisation = null;
                model.SponsorOrgSearch = new OrganisationSearchViewModel();
                break;

            case "fromdate":
                model.FromDay = model.FromMonth = model.FromYear = null;
                break;

            case "todate":
                model.ToDay = model.ToMonth = model.ToYear = null;
                break;

            case "country":
                if (!string.IsNullOrEmpty(value) && model.Country != null)
                {
                    model.Country = model.Country
                        .Where(c => !string.Equals(c, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                break;

            case "modificationtypes":
                if (!string.IsNullOrEmpty(value) && model.ModificationTypes != null)
                {
                    model.ModificationTypes = model.ModificationTypes
                        .Where(m => !string.Equals(m, value, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                break;
        }

        // Save updated model to session
        HttpContext.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(model));

        return await ApplyFilters(model);
    }

    /// <summary>
    ///     Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    [Route("/approvals/searchorganisations", Name = "approvals:searchorganisations")]
    public async Task<IActionResult> SearchOrganisations(ApprovalsSearchModel model, string? role, int? pageSize)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // store the irasId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.IrasId, model.IrasId);

        // set the previous, current and next stages
        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.SponsorOrgSearch.SelectedOrganisation = string.Empty;

        // add the search model to temp data to use in the view
        TempData.TryAdd(TempDataKeys.OrgSearch, model.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.SponsorOrgSearch.SearchText) || model.SponsorOrgSearch.SearchText.Length < 3)
        {
            // add model validation error if search text is empty
            ModelState.AddModelError("sponsor_org_search",
                "Please provide 3 or more characters to search sponsor organisation.");

            // save the model state in temp data, to use it on redirects to show validation errors
            // the modelstate will be merged using the action filter ModelStateMergeAttribute
            // only if the TempData has ModelState stored
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);

            // Return the view with the model state errors.
            return Redirect(returnUrl);
        }

        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        // Fetch organisations from the RTS service, with or without pagination.
        var searchResponse = pageSize is null
            ? await rtsService.GetOrganisations(model.SponsorOrgSearch.SearchText!, role)
            : await rtsService.GetOrganisations(model.SponsorOrgSearch.SearchText, role, pageSize.Value);

        // Handle error response from the service.
        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        // Convert the response content to a list of organisation names.
        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        return Redirect(returnUrl);
    }
}