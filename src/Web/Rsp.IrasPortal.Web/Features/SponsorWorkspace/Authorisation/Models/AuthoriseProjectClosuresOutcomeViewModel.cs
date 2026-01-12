using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class AuthoriseProjectClosuresOutcomeViewModel
{
    [Required(ErrorMessage = "Select an outcome")]
    public string? Outcome { get; set; } // "Authorised" | "NotAuthorised"

    // keep your route context so the POST can round-trip
    public string ProjectRecordId { get; set; } = null!;

    public Guid SponsorOrganisationUserId { get; set; }
    public string ActualEndDate { get; set; }
    public string PlannedEndDate { get; set; }
    public int? IrasId { get; set; }
    public string? ShortProjectTitle { get; set; }
}