using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// View model used for selecting the area and specific change in a project modification journey.
/// Inherits basic project metadata from <see cref="BaseProjectModificationViewModel"/>.
/// </summary>
public class AreaOfChangeViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the selected area of change identifier.
    /// </summary>
    public int? AreaOfChangeId { get; set; }

    /// <summary>
    /// Gets or sets the selected specific change identifier associated with the area of change.
    /// </summary>
    public int? SpecificChangeId { get; set; }

    /// <summary>
    /// Gets or sets the list of available area of change options for the dropdown.
    /// </summary>
    public IEnumerable<SelectListItem> AreaOfChangeOptions { get; set; }

    /// <summary>
    /// Gets or sets the list of available specific change options for the selected area of change.
    /// </summary>
    public IEnumerable<SelectListItem> SpecificChangeOptions { get; set; }
}