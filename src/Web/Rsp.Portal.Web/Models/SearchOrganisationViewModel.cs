using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

/// <summary>
/// View model for handling organisation search input as part of the project modification journey.
/// Inherits basic project metadata from <see cref="BaseProjectModificationViewModel"/>.
/// </summary>
public class SearchOrganisationViewModel : BaseProjectModificationViewModel
{
    public OrganisationSearchModel Search { get; set; } = new();
    public IEnumerable<SelectableOrganisationViewModel> Organisations { get; set; } = [];
    public List<string> SelectedOrganisationIds { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}