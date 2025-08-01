﻿using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
public partial class ProjectModificationController
{
    /// <summary>
    /// Returns the view for selecting a participating organisation.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ParticipatingOrganisation(int pageNumber = 1, int pageSize = 10, List<string>? selectedOrganisationIds = null)
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new SearchOrganisationViewModel
        {
            SelectedOrganisationIds = selectedOrganisationIds ?? []
        });

        if (TempData.Peek(TempDataKeys.OrganisationSearchModel) is string json)
        {
            viewModel.Search = JsonSerializer.Deserialize<OrganisationSearchModel>(json)!;
        }

        var response = string.IsNullOrEmpty(viewModel.Search.SearchNameTerm) ?
            await rtsService.GetOrganisations(null, pageNumber, pageSize) :
            await rtsService.GetOrganisationsByName(viewModel.Search.SearchNameTerm, null, pageNumber, pageSize);

        viewModel.Organisations = response?.Content?.Organisations?.Select(dto => new SelectableOrganisationViewModel
        {
            Organisation = new OrganisationModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Address = dto.Address,
                CountryName = dto.CountryName,
                Type = dto.Type
            }
        }).ToList() ?? [];

        foreach (var org in viewModel.Organisations)
        {
            if (selectedOrganisationIds?.Contains(org.Organisation.Id) == true)
            {
                org.IsSelected = true;
            }
        }

        viewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, response?.Content?.TotalCount ?? 0)
        {
            SortDirection = SortDirections.Ascending,
            SortField = nameof(OrganisationModel.Name),
            FormName = "organisation-selection"
        };

        return View(nameof(SearchOrganisation), viewModel);
    }

    /// <summary>
    /// Handles search form submission for participant organisation lookup.
    /// Validates the search model and returns the view with errors if needed.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SearchOrganisation(SearchOrganisationViewModel model)
    {
        var validationResult = await searchOrganisationValidator.ValidateAsync(new ValidationContext<SearchOrganisationViewModel>(model));
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(nameof(SearchOrganisation), model);
        }

        TempData[TempDataKeys.OrganisationSearchModel] = JsonSerializer.Serialize(model.Search);
        return RedirectToAction(nameof(ParticipatingOrganisation));
    }
}