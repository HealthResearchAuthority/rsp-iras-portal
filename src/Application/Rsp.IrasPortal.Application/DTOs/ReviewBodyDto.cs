namespace Rsp.IrasPortal.Application.DTOs;

public class ReviewBodyDto
{
    public Guid Id { get; set; }
    public string OrganisationName { get; set; }
    public string EmailAddress { get; set; }
    public List<string> Countries { get; set; } = new();
    public string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}