using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;

namespace Rsp.IrasPortal.Application.DTOs;

public sealed class SponsorUserAuthorisationResult
{
    private SponsorUserAuthorisationResult(bool isAuthorised, IActionResult? failureResult)
    {
        IsAuthorised = isAuthorised;
        FailureResult = failureResult;
    }

    public bool IsAuthorised { get; }
    public IActionResult? FailureResult { get; }

    public Guid? CurrentUserId { get; private set; }

    // NEW: optional context
    public IReadOnlyList<SponsorOrganisationDto> SponsorOrganisations { get; private set; } = Array.Empty<SponsorOrganisationDto>();
    public int SponsorOrganisationCount { get; private set; }
    public string? SponsorOrganisationName { get; private set; }
    public string? RtsId { get; private set; }

    public static SponsorUserAuthorisationResult Fail(IActionResult failureResult)
        => new(false, failureResult);

    public static SponsorUserAuthorisationResult Ok(Guid currentUserId)
        => new(true, null) { CurrentUserId = currentUserId };

    public SponsorUserAuthorisationResult WithSponsorOrganisations(IEnumerable<SponsorOrganisationDto> sponsorOrganisations)
    {
        var list = sponsorOrganisations?.ToList() ?? new List<SponsorOrganisationDto>();
        SponsorOrganisations = list;
        SponsorOrganisationCount = list.Count;
        return this;
    }

    public SponsorUserAuthorisationResult WithSelectedOrganisation(string rtsId, string? sponsorOrganisationName)
    {
        RtsId = rtsId;
        SponsorOrganisationName = sponsorOrganisationName;
        return this;
    }
}
