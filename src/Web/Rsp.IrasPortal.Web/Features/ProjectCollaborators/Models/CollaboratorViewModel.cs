namespace Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;

public class CollaboratorViewModel
{
    public string? ProjectRecordId { get; set; }

    public string? Id { get; set; }

    public string? Email { get; set; }

    public string? UserId { get; set; }

    public string? ProjectAccessLevel { get; set; }

    public bool IsOwner { get; set; }

    public bool Self { get; set; }
}