namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class GetAreaOfChangesResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this area of change.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the area of change.
    /// </summary>
    public string Name { get; set; }

    public IEnumerable<ModificationSpecificAreaOfChangeDto>? ModificationSpecificAreaOfChanges { get; set; }
}