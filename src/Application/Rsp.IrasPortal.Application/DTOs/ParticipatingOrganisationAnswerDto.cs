using Rsp.Portal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
/// Request DTO representing a project modification.
/// </summary>
public class ParticipatingOrganisationAnswerDto : RespondentAnswerDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Modification Participating Organisation Id
    /// </summary>
    public Guid ModificationParticipatingOrganisationId { get; set; }
}