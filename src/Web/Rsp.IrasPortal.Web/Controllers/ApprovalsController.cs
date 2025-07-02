using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;

[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Policy = "IsUser")]
public class ApprovalsController(
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
        ValidationResult? validationResult = await validator.ValidateAsync(model);

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
        }

        switch (key.ToLower())
        {
            case "iras id":
                model.IrasId = null;
                break;

            case "chief investigator name":
                model.ChiefInvestigatorName = null;
                break;

            case "project title":
                model.ShortProjectTitle = null;
                break;

            case "from date":
                model.FromDay = model.FromMonth = model.FromYear = null;
                break;

            case "to date":
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

            case "modification types":
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
}