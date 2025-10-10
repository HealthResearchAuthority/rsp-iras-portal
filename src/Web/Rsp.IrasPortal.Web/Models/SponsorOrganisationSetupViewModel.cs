using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Models;

[ExcludeFromCodeCoverage]
public class SponsorOrganisationSetupViewModel
{
    public OrganisationSearchViewModel SponsorOrgSearch { get; set; } = new();

    [ModelBinder(Name = "input-autocomplete")]
    public string? SponsorOrganisation { get; set; }
}