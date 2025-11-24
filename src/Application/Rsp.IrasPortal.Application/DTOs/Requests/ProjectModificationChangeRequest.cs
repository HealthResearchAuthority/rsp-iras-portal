using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to change a project modification, including details about the area of change,
/// status, and user information.
/// </summary>
public record ProjectModificationChangeRequest
{
    /// <summary>
    /// The unique identifier for the project modification change record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique identifier for the project modification record.
    /// </summary>
    public Guid ProjectModificationId { get; set; }

    /// <summary>
    /// Gets or sets the general area where the change is being made.
    /// </summary>
    public string AreaOfChange { get; set; } = null!;

    /// <summary>
    /// Gets or sets the specific area within the general area where the change is being made.
    /// </summary>
    public string SpecificAreaOfChange { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current status of the project modification.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Overall ranking type of the modification
    /// </summary>
    public string? ModificationType { get; set; }

    /// <summary>
    /// Overall ranking category of the modification
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Overall ranking review type of the modification
    /// </summary>
    public string? ReviewType { get; set; }

    /// <summary>
    /// Gets or sets the user identifier of the person who created the project modification request.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user identifier of the person who last updated the project modification request.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// Overall ranking type of the modification
    /// </summary>
    public (string Substantiality, int Order) ModificationSubstantiality { get; set; }

    /// <summary>
    /// Overall ranking category of the modification
    /// </summary>
    public (string Category, int Order) Categorisation { get; set; }

    /// <summary>
    /// Gets or sets the date the modification change was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the date the modification change was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Prevents the Categorisation property from being serialized.
    /// </summary>
    /// <remarks>ShouldSerialize[PropertyName] is a convention used by JsonSerializer to determine whether a property should be serialized.</remarks>
    public static bool ShouldSerializeCategorisation()
    {
        return false;
    }

    /// <summary>
    /// Prevents the ModificationSubstantiality property from being serialized.
    /// </summary>
    /// <remarks>ShouldSerialize[PropertyName] is a convention used by JsonSerializer to determine whether a property should be serialized.</remarks>
    public static bool ShouldSerializeModificationSubstantiality()
    {
        return false;
    }

    /// <summary>
    /// Updates this change request's ranking-related fields from a computed ranking.
    /// </summary>
    /// <param name="ranking">The computed ranking for this change.</param>
    public void UpdateRanking(RankingOfChangeResponse ranking)
    {
        var modificationType = ranking?.ModificationType?.Substantiality ?? Ranking.NotAvailable;

        // If Non-notifiable, force Category to N/A; otherwise use computed category or N/A fallback.
        var category = string.Equals
        (
            modificationType, Ranking.ModificationTypes.NonNotifiable, StringComparison.OrdinalIgnoreCase
        ) ? Ranking.CategoryTypes.NA : (ranking?.Categorisation?.Category ?? Ranking.NotAvailable);

        ModificationType = modificationType;
        Category = category;
        ReviewType = ranking?.ReviewType ?? Ranking.NotAvailable;

        // Tuple payloads used server-side for ordering; default order 0 when not supplied.
        ModificationSubstantiality = (modificationType, ranking?.ModificationType?.Order ?? 0);

        // If category is N/A due to Non-notifiable, force order to last (6) per CMS configuration.
        Categorisation =
        (
            category,
            category == Ranking.CategoryTypes.NA ? 6 : (ranking?.Categorisation?.Order ?? 0)
        );
    }
}