using Rsp.Portal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.MemberManagement.Models;

public class RecMemberViewModel
{
    public string UserId { get; set; } = null!;
    public string? Title { get; set; }
    public string? FirstName { get; set; } = null!;
    public string? LastName { get; set; } = null!;
    public string? EmailAddress { get; set; } = null!;
    public string? Organisation { get; set; }
    public string? JobTitle { get; set; }
    public string? RecTelephoneNumber { get; set; }
    public string? CommitteeRole { get; set; }
    public string? Designation { get; set; }

    public bool MemberLeftOrganisation { get; set; } = false;
    public Guid RecId { get; set; }

    public string RecName { get; set; } = null!;
    public bool IsEditMode { get; set; } = false;
    public DateTime? LastUpdated { get; set; }
    public DateTime? DateTimeLeft => DateTimeExtensions.ParseDateValidation(DateTimeLeftDay, DateTimeLeftMonth, DateTimeLeftYear);

    public string? DateTimeLeftDay { get; set; }
    public string? DateTimeLeftMonth { get; set; }
    public string? DateTimeLeftYear { get; set; }
}