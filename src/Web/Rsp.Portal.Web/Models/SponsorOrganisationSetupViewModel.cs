using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Models;

[ExcludeFromCodeCoverage]
public class SponsorOrganisationSetupViewModel
{
    public OrganisationSearchViewModel SponsorOrgSearch { get; set; } = new();

    [ModelBinder(Name = "input-autocomplete")]
    public string? SponsorOrganisation { get; set; }
}