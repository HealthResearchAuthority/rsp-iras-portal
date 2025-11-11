using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

public class AuthoriseOutcomeViewModel : ModificationDetailsViewModel
{
    [Required(ErrorMessage = "Select an outcome")]
    public string? Outcome { get; set; } // "Authorised" | "NotAuthorised"

    // keep your route context so the POST can round-trip
    public string ProjectRecordId { get; set; } = "";
    public string IrasId { get; set; } = "";
    public string ShortTitle { get; set; } = "";
    public Guid ProjectModificationId { get; set; }
    public Guid SponsorOrganisationUserId { get; set; }
}