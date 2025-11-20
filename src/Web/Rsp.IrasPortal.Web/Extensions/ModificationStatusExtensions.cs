using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Extensions;

[ExcludeFromCodeCoverage]
public static class ModificationStatusExtensions
{
    public static string ToBackstageDisplayStatus(this string status, string? reviewerName)
    {
        // Only care about WithReviewBody — everything else returns unchanged
        if (!status.Equals(nameof(ModificationStatus.WithReviewBody), StringComparison.OrdinalIgnoreCase))
            return status;

        // With reviewer name → Review in progress
        if (!string.IsNullOrWhiteSpace(reviewerName))
            return ModificationStatus.ReviewInProgress;

        // Without reviewer name → Received
        return ModificationStatus.Received;
    }
}