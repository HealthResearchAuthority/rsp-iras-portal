namespace Rsp.Portal.Web.Models;

public class SelectableOrganisationViewModel
{
    public OrganisationModel Organisation { get; set; } = null!;
    public bool IsSelected { get; set; }
}