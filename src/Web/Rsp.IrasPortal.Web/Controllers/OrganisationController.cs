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
[Authorize(Policy = "IsUser")]
public class OrganisationController
(
    IRtsService rtsService
) : Controller
{
    /// <summary>
    /// Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="name">The name of the organisation to search for.</param>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> GetOrganisations(string name, string? role, int? pageSize)
    {
        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        // Fetch organisations from the RTS service, with or without pagination.
        var response = pageSize is null ?
            await rtsService.GetOrganisations(name, role) :
            await rtsService.GetOrganisations(name, role, pageSize.Value);

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