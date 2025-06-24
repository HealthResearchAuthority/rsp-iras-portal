namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel for searching organisations.
/// </summary>
public class OrganisationSearchViewModel
{
    /// <summary>
    /// Selected Organisation
    /// </summary>
    public string? SelectedOrganisation { get; set; }

    /// <summary>
    /// Organisation search text
    /// </summary>
    public string? SearchText { get; set; }
}