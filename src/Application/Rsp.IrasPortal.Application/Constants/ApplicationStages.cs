namespace Rsp.IrasPortal.Application.Constants;

/// <summary>
/// Research Application Stages
/// </summary>
public struct ApplicationStages
{
    public const string Initiate = "Initiate";
    public const string ProjectFilter = "Project Filter";
    public const string ProjectDetails = "Project Details";
    public const string Student = "Student";
    public const string ResearchBioresouces = "Research Bioresouces";
    public const string Ctimp = "CTIMP";
    public const string Devices = "Devices";
    public const string IonisingRadiation = "Ionising Radiation";
    public const string Tissue = "Tissue";
    public const string AdultLackingCapacity = "Adult Lacking Capacity";
    public const string Children = "Children";
    public const string Booking = "Booking";
}

public struct QuestionSetColumns
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
}

public struct RulesColumns
{
    public const string RuleId = "RuleId";
    public const string Sequence = "Sequence";
    public const string Operator = "Operator";
    public const string QuestionId = "QuestionId";
    public const string ParentQuestionId = "ParentQuestionId";
    public const string Description = "Description";
    public const string ConditionComparison = "Comparator";
    public const string ConditionOptionsCountOperator = "ConditionOperator";
    public const string ConditionParentOptions = "ParentOption";
}