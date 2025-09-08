namespace Rsp.IrasPortal.Web.Models;

public class ModificationChangeModel
{
    public Guid ModificationChangeId { get; set; }
    public string? ModificationType { get; set; }
    public string? Category { get; set; }
    public string? ReviewType { get; set; }
    public string AreaOfChangeName { get; set; } = null!;
    public string SpecificChangeName { get; set; } = null!;
    public string? SpecificChangeAnswer { get; set; }
    public string ChangeStatus { get; set; } = null!;
}