using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Helpers;

public static class CreateNewModificationValidation
{
    public static bool ValidateNewModification(
    IEnumerable<ModificationsDto> existingModifications,
    string newStatus)
    {
        // Active statuses only
        var activeStatuses = new[]
        {
            ModificationStatus.InDraft,
            ModificationStatus.WithSponsor,
            ModificationStatus.WithReviewBody
        };

        // Active modification only
        var activeModifications = existingModifications
                     .Where(m => activeStatuses.Contains(m.Status))
                     .ToList();

        // Only one In-draft allowed
        if (newStatus == ModificationStatus.InDraft &&
            activeModifications.Any(m => m.Status == ModificationStatus.InDraft))
        {
            return false;
        }

        // Max one in-flight modifications.
        int inFlightModification = activeModifications.Count(m =>
            m.Status == ModificationStatus.WithSponsor ||
            m.Status == ModificationStatus.WithReviewBody
        );

        // You already have two in-flight modifications. No further modifications allowed
        if (inFlightModification >= 2)
            return (false);

        // Duplicate status not allowed
        if (activeModifications.Any(m => m.Status == newStatus))
            return (false);

        // If there's already 1 in-flight (with sponsor / review body), activeMods.Count == 1, We allow creation unless it violates duplicate rule.
        // If in-flight exists and the new one would be a duplicate, duplicate rule blocks it.
        // If there are 2 actives already, max-count rule blocks it.

        return true;
    }
}