namespace Rsp.Portal.Web.Models;

/// <summary>
/// ViewModel for searching organisations.
/// </summary>
public class OrganisationSearchViewModel
{
    /// <summary>
    /// Selected Organisation id
    /// </summary>
    public string? SelectedOrganisation { get; set; }

    /// <summary>
    /// Organisation search text
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// The name of selected organisation
    /// </summary>
    public string? DisplayName { get; set; }
}