using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
/// Represents questions response returned by the QuestionSet API
/// </summary>
public record QuestionsResponse
{
    /// <summary>
    /// Question Id
    /// </summary>
    public string QuestionId { get; set; } = null!;

    /// <summary>
    /// CategoryId of the question e.g. A, B, C1
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// SectionId of the question prefixed with IQT
    /// </summary>
    public string SectionId { get; set; } = null!;

    /// <summary>
    /// Section name for the question e.g. Project Scope, Research Location etc
    /// </summary>
    public string Section { get; set; } = null!;

    /// <summary>
    /// The sequence number for the question
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Indicates if the question is a modification question
    /// </summary>
    public bool IsModificationQuestion { get; set; }

    /// <summary>
    /// Heading of the question e.g. 1, 2, 2a, 2b..
    /// </summary>
    public string Heading { get; set; } = null!;

    /// <summary>
    /// Question Text
    /// </summary>
    public string QuestionText { get; set; } = null!;

    /// <summary>
    /// Type of the question e.g. Boolean, Checkbox etc
    /// </summary>
    public string QuestionType { get; set; } = null!;

    /// <summary>
    /// DataType used to render the UI component e.g. Checkbox, Radio button, Date
    /// </summary>
    public string DataType { get; set; } = null!;

    /// <summary>
    /// Indicates if the question is mandatory or not
    /// </summary>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Indicates if the question is optional or not
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Potential answers of the question for single or multiple choice type question
    /// </summary>
    public IList<AnswerDto> Answers { get; set; } = [];

    /// <summary>
    /// Rules associated with the question. Drives the conditionality of the question
    /// </summary>
    public IList<RuleDto> Rules { get; set; } = [];

    /// <summary>
    /// When the question was created
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// When this version of question is no longer valid
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Version of the question
    /// </summary>
    public string? VersionId { get; set; }

    /// <summary>
    /// Short question text to display to the user
    /// </summary>
    public string ShortQuestionText { get; set; } = null!;

    public IList<ContentComponent> GuidanceComponents { get; set; } = [];
}