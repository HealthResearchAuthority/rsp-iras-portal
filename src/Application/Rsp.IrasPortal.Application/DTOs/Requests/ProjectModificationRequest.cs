using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to modify a project, including identifiers, status, and user information.
/// </summary>
public record ProjectModificationRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the project record identifier.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sequential number of the modification.
    /// </summary>
    public int ModificationNumber { get; set; }

    /// <summary>
    /// Gets or sets the modification identifier.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the modification.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID who created the modification.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user ID who last updated the modification.
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date the modification was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the date the modification was last updated.
    /// </summary>
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

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
    /// Gets or sets the identifier of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerId { get; set; }

    /// <summary>
    /// Gets or sets the email of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerEmail { get; set; }

    /// <summary>
    /// Gets or sets the name of the reviewer assigned to this modification, if any.
    /// </summary>
    public string? ReviewerName { get; set; }

    /// <summary>
    /// Gets or sets the submission date.
    /// This date is populated when a researcher clicks send to sponsor from the Reveiw all changes page, the actual status is With Sponsor
    /// </summary>
    public DateTime? SentToSponsorDate { get; set; }

    public DateTime? SentToRegulatorDate { get; set; }

    /// <summary>
    /// Gets or sets the list of changes associated with this project modification.
    /// </summary>
    public List<ProjectModificationChangeRequest> ProjectModificationChanges { get; set; } = [];

    public void UpdateOverAllRanking()
    {
        var modificationTypes =
            (from change in ProjectModificationChanges
             where change.ModificationSubstantiality.Order > 0
             select change.ModificationSubstantiality).ToList();

        var categories =
            (from change in ProjectModificationChanges
             where change.Categorisation.Order > 0
             select change.Categorisation).ToList();

        var reviewTypes =
            (from change in ProjectModificationChanges
             select change.ReviewType).ToList();

        // overall modification ranking
        ModificationType = modificationTypes.Count != 0 ?
            modificationTypes.MinBy(mt => mt.Order).Substantiality :
            Ranking.NotAvailable;

        // check if have categories B and C but not A, then overall category is B/C
        if (categories.Any(c => c.Category == Ranking.CategoryTypes.B) &&
            categories.Any(c => c.Category == Ranking.CategoryTypes.C) &&
            !categories.Any(c => c.Category == Ranking.CategoryTypes.A))
        {
            Category = Ranking.CategoryTypes.BC;
        }
        else
        {
            Category = categories.Count != 0 ?
            categories.MinBy(mt => mt.Order).Category :
            Ranking.NotAvailable;
        }

        ReviewType = reviewTypes.FirstOrDefault(r => r.Equals(Ranking.ReviewTypes.ReviewRequired)) ??
                     reviewTypes.FirstOrDefault(r => r.Equals(Ranking.ReviewTypes.NoReviewRequired)) ??
                     Ranking.NotAvailable;
    }
}