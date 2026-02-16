using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Application.DTOs;

public sealed class SponsorUserAuthorisationResult
{
    public Guid? CurrentUserId { get; init; }
    public IActionResult? FailureResult { get; init; }
    public bool IsAuthorised => FailureResult is null && CurrentUserId.HasValue;

    public static SponsorUserAuthorisationResult Fail(IActionResult result) => new() { FailureResult = result };

    public static SponsorUserAuthorisationResult Ok(Guid gid) => new() { CurrentUserId = gid };
}