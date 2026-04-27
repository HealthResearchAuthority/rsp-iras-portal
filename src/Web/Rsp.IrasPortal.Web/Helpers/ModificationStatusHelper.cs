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
            ModificationStatus.ResponseReviseAndAuthorise => ModificationStatus.ResponseWithSponsor,
            _ => status
        };
    }

    public static string? ToDocumentUploadUiStatus(string? status)
    {
        return status switch
        {
            DocumentStatus.RequestRevisions => DocumentStatus.Uploaded,
            DocumentStatus.ReviseAndAuthorise => DocumentStatus.WithSponsor,
            DocumentStatus.ResponseReviseAndAuthorise => DocumentStatus.ResponseWithSponsor,
            _ => status
        };
    }
}