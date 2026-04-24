namespace Rsp.IrasPortal.Web.Models;

public class Collaborator
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Access { get; set; } = null!;
    public bool IsOwner { get; set; }
    public bool Self { get; set; }
}