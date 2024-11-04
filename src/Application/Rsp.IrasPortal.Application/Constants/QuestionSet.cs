namespace Rsp.IrasPortal.Application.Constants;

public class SheetNames
{
    public const string A = nameof(A);
    public const string B = nameof(B);
    public const string C1 = nameof(C1);
    public const string C2 = nameof(C2);
    public const string C3 = nameof(C3);
    public const string C4 = nameof(C4);
    public const string C5 = nameof(C5);
    public const string C6 = nameof(C6);
    public const string C7 = nameof(C7);
    public const string C8 = nameof(C8);
    public const string D = nameof(D);
    public const string AnswerOptions = "App2 AnswerOptions";
    public const string Rules = "App4 Rules";

    public static readonly string[] All = [
        A,
        //B,
        C1,
        //C2,
        //C3,
        C4,
        //C5,
        C6,
        C7,
        C8,
        //D,
        AnswerOptions,
        Rules
    ];
}

public class ModuleColumns
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
        Answers
    ];
}

public class RulesColumns
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

public class AnswerOptionsColumns
{
    public const string OptionId = "OptionID";
    public const string OptionText = "OptionText";

    public static readonly string[] All = [
        OptionId,
        OptionText
    ];
};