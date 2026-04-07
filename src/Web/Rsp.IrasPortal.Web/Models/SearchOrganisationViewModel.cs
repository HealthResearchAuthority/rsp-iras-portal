using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

/// <summary>
/// View model for handling organisation search input as part of the project modification journey.
/// Inherits basic project metadata from <see cref="BaseProjectModificationViewModel"/>.
/// </summary>
public class SearchOrganisationViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the search criteria for organisations.
    /// </summary>
    public OrganisationSearchModel Search { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of organisations returned from the search.
    /// </summary>
    public List<SelectableOrganisationViewModel>? Organisations { get; set; }

    /// <summary>
    /// Gets or sets the list of selected organisation IDs.
    /// </summary>
    public List<string> SelectedOrganisationsIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the pagination information for the search results.
    /// </summary>
    public PaginationViewModel? Pagination { get; set; }
}