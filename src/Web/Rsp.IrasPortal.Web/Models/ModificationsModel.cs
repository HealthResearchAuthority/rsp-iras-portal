using System.Diagnostics.CodeAnalysis;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class ModificationsModel
{
    public string ModificationId { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public string? ModificationType { get; set; } = null!;
    public string ChiefInvestigator { get; set; } = null!;
    public string LeadNation { get; set; } = null!;
    public string SponsorOrganisation { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}