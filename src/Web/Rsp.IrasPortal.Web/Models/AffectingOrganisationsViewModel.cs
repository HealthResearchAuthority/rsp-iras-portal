namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel representing organisations and resources affecting a process or event.
/// </summary>
public class AffectingOrganisationsViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the list of participating organisations' locations.
    /// </summary>
    public List<string> SelectedLocations { get; set; } = [];

    /// <summary>
    /// Gets or sets the list indicating if all or some organisations are affected.
    /// </summary>
    public string? SelectedAffectedOrganisations { get; set; }

    /// <summary>
    /// Gets or sets the list indicating if affected organisations require additional resources.
    /// </summary>
    public string? SelectedAdditionalResources { get; set; }

    /// <summary>
    /// Gets the dictionary of participating organisations' locations.
    /// Key: Organisation code, Value: Location name.
    /// </summary>
    public IReadOnlyDictionary<string, string> ParticipatingOrganisationsLocations { get; } =
        new Dictionary<string, string>
        {
            { "OPT0018", "England" },
            { "OPT0019", "Northern Ireland" },
            { "OPT0020", "Scotland" },
            { "OPT0021", "Wales" },
        };

    /// <summary>
    /// Gets the dictionary of affected organisations options.
    /// Key: Option code, Value: Option description ("All" or "Some").
    /// </summary>
    public IReadOnlyDictionary<string, string> AffectedOrganisations { get; } =
        new Dictionary<string, string>
        {
            { "OPT0323", "All" },
            { "OPT0324", "Some" }
        };

    /// <summary>
    /// Gets the dictionary of additional resources options.
    /// Key: Option code, Value: Option description ("Yes" or "No").
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalResources { get; } =
        new Dictionary<string, string>
        {
            { "OPT0004", "Yes" },
            { "OPT0005", "No" }
        };
}