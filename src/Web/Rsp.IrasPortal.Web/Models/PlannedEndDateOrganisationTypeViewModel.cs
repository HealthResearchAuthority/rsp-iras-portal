namespace Rsp.IrasPortal.Web.Models;

public class PlannedEndDateOrganisationTypeViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the list of selected organisation type identifiers (e.g., NHS/HSC, Non-NHS/HSC).
    /// These correspond to the keys defined in <see cref="OrganisationTypes"/>.
    /// </summary>
    public List<string> SelectedOrganisationTypes { get; set; } = [];

    /// <summary>
    /// A static, immutable dictionary that defines the available organisation types that a user can select from.
    /// The key represents a unique identifier (e.g., "OPT0025"), and the value is the human-readable name
    /// shown on the user interface (e.g., "NHS/HSC").
    /// </summary>
    public IReadOnlyDictionary<string, string> OrganisationTypes { get; } =
        new Dictionary<string, string>
        {
            { "OPT0025", "NHS/HSC" },
            { "OPT0026", "Non-NHS/HSC" }
        };
}