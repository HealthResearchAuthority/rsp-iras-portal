namespace Rsp.IrasPortal.Application.DTOs;

public class SponsorOrganisationDto
{
    public Guid Id { get; set; }
    public string SponsorOrganisationName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public List<string> Countries { get; set; } = [];
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public IEnumerable<SponsorOrganisationUserDto>? Users { get; set; } = [];
}