using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Application.DTOs.Requests;

public class OrganisationsSearchRequest
{
    public string? SearchNameTerm { get; set; }
    public List<string> ExcludingRoles { get; set; } = [OrganisationRoles.Sponsor];
    public List<string> Countries { get; set; } = [];
    public List<string> OrganisationTypes { get; set; } = [];
    public List<string> OrganisationStatuses { get; set; } = [];
}