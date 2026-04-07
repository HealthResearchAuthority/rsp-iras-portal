using Rsp.IrasPortal.Application.DTOs;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;

public class OrganisationDetailsViewModel : QuestionnaireViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the organisation.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the RTS organisation Id.
    /// </summary>
    public string OrganisationId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the organisation.
    /// </summary>
    public string OrganisationName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the address of the organisation.
    /// </summary>
    public string OrganisationAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the country name of the organisation.
    /// </summary>
    public string OrganisationCountryName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the organisation.
    /// </summary>
    public string OrganisationType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the organisation details.
    /// </summary>
    public string? DetailsStatus { get; set; }

    /// <summary>
    /// Gets or sets the IRAS (Integrated Research Application System) identifier for the project.
    /// </summary>
    public string IrasId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the short title of the project.
    /// </summary>
    public string? ShortTitle { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the current modification.
    /// </summary>
    public string? ModificationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the current project modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title displayed on the page for context.
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// Gets or sets the Project Record Id.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    public List<ParticipatingOrganisationAnswerDto> ToAnswersDto()
    {
        return
            Questions
            .ConvertAll(q => new ParticipatingOrganisationAnswerDto
            {
                ModificationParticipatingOrganisationId = Guid.Parse(OrganisationId),
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                VersionId = q.VersionId,
                CategoryId = q.Category,
                SectionId = q.SectionId,
                AnswerText = q.AnswerText,
                OptionType = q.DataType switch
                {
                    "Boolean" or "Radio button" or "Look-up list" or "Dropdown" => "Single",
                    "Checkbox" => "Multiple",
                    _ => null
                },
                SelectedOption = q.SelectedOption,
                Answers = [.. q.Answers
                                .Where(a => a.IsSelected)
                                .Select(ans => ans.AnswerId)]
            });
    }
}