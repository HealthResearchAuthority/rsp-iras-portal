﻿namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// View model for handling organisation search input as part of the project modification journey.
/// Inherits basic project metadata from <see cref="BaseProjectModificationViewModel"/>.
/// </summary>
public class SearchOrganisationViewModel : BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the search term entered by the user to find an organisation.
    /// </summary>
    public string? SearchTerm { get; set; }
}