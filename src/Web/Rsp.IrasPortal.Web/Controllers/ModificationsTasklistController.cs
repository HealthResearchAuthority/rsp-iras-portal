using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "tasklist:[action]")]
[Authorize(Policy = "IsUser")]
public class ModificationsTasklistController(IApplicationsService applicationsService) : Controller
{
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        int pageSize = 20,
        IList<string>? selectedModificationIds = null,
        string? sortField = nameof(ModificationsModel.CreatedAt),
        string? sortDirection = SortDirections.Ascending)
    {
        var model = new ModificationsTasklistViewModel();

        var modQuery = new ModificationSearchRequest()
        {
            Country = ["England"],
        };

        var querySortField = sortField;
        var querySortDirection = sortDirection;

        if (sortField == nameof(ModificationsModel.DaysSinceSubmission))
        {
            querySortField = nameof(ModificationsModel.CreatedAt);
            querySortDirection = sortDirection == SortDirections.Ascending
                ? SortDirections.Descending
                : SortDirections.Ascending;
        }

        var result = await applicationsService.GetModifications(modQuery, pageNumber, pageSize, querySortField, querySortDirection);
        model.Modifications = result?.Content?.Modifications?
            .Select(dto => new TaskListModificationViewModel
            {
                Modification = new ModificationsModel
                {
                    ModificationId = dto.ModificationId,
                    ShortProjectTitle = dto.ShortProjectTitle,
                    ModificationType = dto.ModificationType,
                    ChiefInvestigator = dto.ChiefInvestigator,
                    LeadNation = dto.LeadNation,
                    SponsorOrganisation = dto.SponsorOrganisation,
                    CreatedAt = dto.CreatedAt
                }
            })
            .ToList() ?? [];

        foreach (var mod in model.Modifications)
        {
            if (selectedModificationIds?.Contains(mod.Modification.ModificationId) == true)
            {
                mod.IsSelected = true;
            }
        }

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, result?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField
        };

        return View(model);
    }
}