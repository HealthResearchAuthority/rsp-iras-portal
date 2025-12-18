using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;

public class AuthoriseModificationsOutcomeViewModel : ModificationDetailsViewModel
{
    [Required(ErrorMessage = "Select an outcome")]
    public string? Outcome { get; set; } // "Authorised" | "NotAuthorised"

    // keep your route context so the POST can round-trip
    public Guid ProjectModificationId { get; set; }

    public Guid SponsorOrganisationUserId { get; set; }
}