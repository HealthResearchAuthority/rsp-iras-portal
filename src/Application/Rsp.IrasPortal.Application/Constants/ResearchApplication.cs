namespace Rsp.IrasPortal.Application.Constants;

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

public struct QuestionCategories
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