namespace Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;

public class SearchCollaboratorViewModel
{
    public string ProjectRecordId { get; set; } = null!;

    public string? Email { get; set; }

    public string? UserId { get; set; }

    public bool? CollaboratorFound { get; set; }

    public bool? IsExistingCollaborator { get; set; }

    public bool? InvalidUser { get; set; }

    public string? InvalidUserMessage { get; set; }
}