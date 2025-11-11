using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

namespace Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

public class QuestionModel
{
    public string? Name { get; set; }
    public string? ShortName { get; set; }

    /// <summary>
    /// Auto generate Id by the CMS
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Manually created Id by the user
    /// </summary>
    public string QuestionId { get; set; } = null!;

    public string Key { get; set; } = null!;
    public string? Label { get; set; }
    public string? Conformance { get; set; }
    public string? QuestionFormat { get; set; }
    public string? AnswerDataType { get; set; }
    public string? CategoryId { get; set; }
    public string? Version { get; set; }
    public bool ShowOriginalAnswer { get; set; }
    public int Sequence { get; set; }
    public int SectionSequence { get; set; }
    public string ShowAnswerOn { get; set; } = string.Empty;
    public string? SectionGroup { get; set; }
    public int SequenceInSectionGroup { get; set; }
    public bool IsEditable { get; set; }

    public IList<AnswerModel> Answers { get; set; } = [];
    public IList<RuleModel> ValidationRules { get; set; } = [];
    public IList<ComponentContent> GuidanceComponents { get; set; } = [];

    public string? NhsInvolvment { get; set; }
    public string? NonNhsInvolvment { get; set; }
    public bool AffectedOrganisations { get; set; }
    public bool RequireAdditionalResources { get; set; }
    public bool UseAnswerForNextSection { get; set; }
}