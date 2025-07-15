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

        // use this query to add filters
        var modQuery = new ModificationSearchRequest();

        var result = await applicationsService.GetModifications(modQuery, pageNumber, pageSize, sortField, sortDirection);
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
            if (selectedModificationIds != null && selectedModificationIds.Contains(mod.Modification.ModificationId))
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