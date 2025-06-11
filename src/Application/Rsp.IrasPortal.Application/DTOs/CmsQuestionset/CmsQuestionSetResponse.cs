namespace Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

public class CmsQuestionSetResponse
{
    public string Id { get; set; } = null!;
    public string Version { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public string? Status { get; set; }
    public IList<SectionModel> Sections { get; set; } = new List<SectionModel>();
}

public class SectionModel
{
    public string SectionName { get; set; }
    public string Id { get; set; }
    public IList<QuestionModel> Questions { get; set; } = new List<QuestionModel>();
}

public class QuestionModel
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Key { get; set; }
    public string Label { get; set; }
    public bool IsMandatory { get; set; }
    public string QuestionFormat { get; set; }
    public string AnswerDataType { get; set; }
    public IList<AnswerModel> Answers { get; set; } = new List<AnswerModel>();
    public IList<RuleModel> ValidationRules { get; set; } = new List<RuleModel>();
}

public class AnswerModel
{
    public string OptionName { get; set; }
    public string Id { get; set; }
    public string Key { get; set; }
}

public class ConditionModel
{
    public string Operator { get; set; }
    public string Mode { get; set; }
    public bool Negate { get; set; }
    public string OptionType { get; set; }
    public string Value { get; set; }
    public IList<AnswerModel> ParentOptions { get; set; }
}

public class RuleModel
{
    public IList<ConditionModel> Conditions { get; set; }
    public string Description { get; set; }
    public string Mode { get; set; }
    public QuestionModel ParentQuestion { get; set; }
    public string QuestionId { get; set; }
}