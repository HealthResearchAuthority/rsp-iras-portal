using Rsp.Portal.Application.Constants;

namespace Rsp.Portal.Web.Helpers;

public static class ModificationStatusHelper
{
    public static string? ToUiStatus(string? status)
    {
        return status switch
        {
            ModificationStatus.RequestRevisions => ModificationStatus.InDraft,
            ModificationStatus.ReviseAndAuthorise => ModificationStatus.WithSponsor,
            _ => status
        };
    }
}