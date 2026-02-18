using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.Logging.Interceptors;

namespace Rsp.Portal.Application.Services;

public interface ISponsorUserAuthorisationService : IInterceptable
{
    Task<SponsorUserAuthorisationResult> AuthoriseAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user);

    Task<SponsorUserAuthorisationResult> AuthoriseWithOrganisationContextAsync(
        Controller controller,
        Guid sponsorOrganisationUserId,
        ClaimsPrincipal user,
        string rtsId);
}