namespace Rsp.IrasPortal.Application.Constants;

public static class SheetNames
{
    public const string ProjectRecord = "project record v1";
    public const string Contents = "contents";
    public const string AnswerOptions = "App2 AnswerOptions";
    public const string Rules = "App4 Rules";

    public static readonly string[] Modules = [
        ProjectRecord
    ];

    public static readonly string[] All = [
        ProjectRecord,
        Contents,
        AnswerOptions,
        Rules
    ];
}

public static class ModuleColumns
{
    public const string QuestionId = "Unique ID";
    public const string Category = "Category";
    public const string Section = "Section";
    public const string Sequence = "UI display sequence";
    public const string Heading = "Proposed Q#";
    public const string QuestionText = "Field label";
    public const string QuestionType = "Field type";
    public const string Conformance = "Conformance";
    public const string Rules = "Data and visibility rules";
    public const string DataType = "Data type";
    public const string Answers = "Values";
    public const string ShortQuestionText = "Short field label";

    public static readonly string[] All = [
        QuestionId,
        Category,
        Section,
        Sequence,
        Heading,
        QuestionText,
        QuestionType,
        Conformance,
        Rules,
        DataType,
        Answers,
        ShortQuestionText
    ];
}

public static class RulesColumns
{
    public const string RuleId = "RuleId";
    public const string Sequence = "Sequence";
    public const string Mode = "Mode";
    public const string QuestionId = "QuestionId";
    public const string ParentQuestionId = "ParentQuestionId";
    public const string Description = "Description";
    public const string ConditionMode = "ConditionMode";
    public const string ConditionOperator = "ConditionOperator";
    public const string ConditionValue = "ConditionValue";
    public const string ConditionNegate = "ConditionNegate";
    public const string ConditionParentOptions = "ConditionParentOptions";
    public const string ConditionOptionType = "ConditionOptionType";
    public const string ConditionDescription = "ConditionDescription";
    public const string ConditionIsApplicable = "ConditionIsApplicable";

    public static readonly string[] All = [
        RuleId,
        Sequence,
        Mode,
        QuestionId,
        ParentQuestionId,
        Description,
        ConditionMode,
        ConditionOperator,
        ConditionValue,
        ConditionNegate,
        ConditionParentOptions,
        ConditionOptionType,
        ConditionDescription,
    ];
}

public static class AnswerOptionsColumns
{
    public const string OptionId = "OptionID";
    public const string OptionText = "OptionText";

    public static readonly string[] All = [
        OptionId,
        OptionText
    ];
};

public static class ContentsColumns
{
    public const string Tab = "Tab";
    public const string Category = "Category";

    public static readonly string[] All = [
        Tab,
        Category,
    ];
};