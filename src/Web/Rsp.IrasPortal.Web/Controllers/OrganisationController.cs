using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller for managing organisation-related operations.
/// </summary>
[Route("[controller]/[action]", Name = "org:[action]")]
public class OrganisationController
(
    IRtsService rtsService
) : Controller
{
    /// <summary>
    /// Retrieves a list of sponsor organisations names based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="name">The name of the organisation to search for.</param>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> GetSponsorOrganisationsNames(string name, string? role, int? pageSize = 5, int pageIndex = 1)
    {
        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        // Fetch organisations from the RTS service, with or without pagination.
        var response = await rtsService.GetOrganisationsByName(name, role, pageIndex, pageSize);

        // Handle error response from the service.
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            return this.ServiceError(response);
        }

        // Convert the response content to a list of organisation names.
        var organisations = response.Content.Organisations.ToList();

        return Ok(organisations.Select(org => org.Name));
    }

    /// <summary>
    /// Query organisations by complete or partial name, with optional role filtering and paging
    /// </summary>
    /// <param name="name">The name or partial name of the organisation to search for.</param>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">1-based index of the page to retrieve. Must be greater than 0. If null, will be set to 1 by default.</param>
    /// <param name="pageSize">Optional number of items per page. If null, all matching organisations are returned. Must be greater than 0 if specified.</param>
    /// <returns></returns>
    public async Task<IActionResult> GetOrganisationsByName(string name, string? role, int pageIndex = 1, int? pageSize = 10)
    {
        // Fetch organisations from the RTS service, with or without pagination.
        var response = await rtsService.GetOrganisationsByName(name, role, pageIndex, pageSize);

        // Handle error response from the service.
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            return this.ServiceError(response);
        }

        return Ok(response.Content);
    }

    /// <summary>
    /// Gets all organisations, with optional role filtering and paging
    /// </summary>
    /// <param name="role">Optional role to filter organisations by.</param>
    /// <param name="pageIndex">1-based index of the page to retrieve. Must be greater than 0. If null, will be set to 1 by default.</param>
    /// <param name="pageSize">Optional number of items per page. If null, all matching organisations are returned. Must be greater than 0 if specified.</param>
    /// <returns></returns>
    public async Task<IActionResult> GetOrganisations(string? role, int pageIndex = 1, int? pageSize = 10)
    {
        // Fetch organisations from the RTS service, with or without pagination.
        var response = await rtsService.GetOrganisations(role, pageIndex, pageSize);

        // Handle error response from the service.
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            return this.ServiceError(response);
        }

        return Ok(response.Content);
    }

    /// <summary>
    /// Retrieves details of a specific organisation by its ID.
    /// </summary>
    /// <param name="id">The ID of the organisation to retrieve.</param>
    /// <returns>The organisation details or an error response.</returns>
    public async Task<IActionResult> GetOrganisation(string id)
    {
        // Fetch the organisation details from the RTS service.
        var response = await rtsService.GetOrganisation(id);

        // Handle error response from the service.
        if (!response.IsSuccessStatusCode || response.Content == null)
        {
            return this.ServiceError(response);
        }

        return Ok(response.Content);
    }
}