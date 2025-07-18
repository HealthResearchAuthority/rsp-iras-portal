namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// View model for capturing and displaying information related to the planned end date
/// within a project modification journey.
/// Inherits basic project metadata from <see cref="BaseProjectModificationViewModel"/>.
/// </summary>
public class PlannedEndDateViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the current planned end date of the project.
    /// </summary>
    public DateTime? CurrentPlannedEndDate { get; set; }

    /// <summary>
    /// Gets or sets the new proposed planned end date for the project modification.
    /// </summary>
    public DateTime? NewPlannedEndDate { get; set; }
}