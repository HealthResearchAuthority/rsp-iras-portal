using System.Data;
using System.Diagnostics.CodeAnalysis;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

[ExcludeFromCodeCoverage]
public class QuestionSetBuilder : IQuestionSetBuilder
{
    private readonly QuestionSetDto _questionSet = new();

    /// <inheritdoc/>
    public IQuestionSetBuilder WithVersion(string version)
    {
        _questionSet.Version = new VersionDto
        {
            VersionId = version,
            CreatedAt = DateTime.UtcNow,
            IsDraft = true,
            IsPublished = false
        };

        return this;
    }

    /// <inheritdoc/>
    public IQuestionSetBuilder WithCategories(DataTable contentsTable)
    {
        var categoryDtos = new List<CategoryDto>();

        foreach (DataRow category in contentsTable.Rows)
        {
            var categoryId = category.Field<string>(ContentsColumns.Tab);

            if (categoryId == null || !SheetNames.Modules.Contains(categoryId))
            {
                continue;
            }

            var categoryDto = new CategoryDto
            {
                CategoryId = categoryId,
                CategoryName = category.Field<string>(ContentsColumns.Category) ?? string.Empty,
                VersionId = _questionSet.Version.VersionId,
            };

            categoryDtos.Add(categoryDto);
        }

        _questionSet.Categories = categoryDtos;

        return this;
    }

    /// <inheritdoc/>
    public IQuestionSetBuilder WithQuestions(List<DataTable> moduleTables, DataTable rulesTable, DataTable answerOptionsTable)
    {
        var questionDtos = new List<QuestionDto>();

        foreach (var moduleTable in moduleTables)
        {
            var sectionName = "";

            foreach (DataRow question in moduleTable.Rows)
            {
                var questionId = question.Field<string>(ModuleColumns.QuestionId);

                if (questionId == null || !questionId.StartsWith("IQ"))
                {
                    continue;
                }

                if (questionId.StartsWith("IQT"))
                {
                    sectionName = question.Field<string>(ModuleColumns.QuestionText) ?? string.Empty;
                    continue;
                }

                var conformance = question.Field<string>(ModuleColumns.Conformance);

                var questionDto = new QuestionDto
                {
                    QuestionId = questionId,
                    Category = question.Field<string>(ModuleColumns.Category) ?? string.Empty,
                    SectionId = question.Field<string>(ModuleColumns.Section) ?? string.Empty,
                    Section = sectionName ?? "",
                    Sequence = Convert.ToInt32(question[ModuleColumns.Sequence]),
                    Heading = Convert.ToString(question[ModuleColumns.Heading]),
                    QuestionText = question.Field<string>(ModuleColumns.QuestionText) ?? string.Empty,
                    ShortQuestionText = question.Field<string>(ModuleColumns.ShortQuestionText) ?? string.Empty,
                    QuestionType = question.Field<string>(ModuleColumns.QuestionType) ?? string.Empty,
                    DataType = question.Field<string>(ModuleColumns.DataType) ?? string.Empty,
                    IsMandatory = conformance == "Mandatory",
                    IsOptional = conformance == "Optional",
                    VersionId = _questionSet.Version.VersionId
                };

                var answerOptionIds = (question.Field<string>(ModuleColumns.Answers) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                questionDto.Answers = GetAnswerOptions(answerOptionsTable, answerOptionIds);

                questionDto.Rules = GetRules(rulesTable, questionId);

                questionDtos.Add(questionDto);
            }
        }

        _questionSet.Questions = questionDtos;

        return this;
    }

    /// <inheritdoc/>
    public QuestionSetDto Build() => _questionSet;

    /// <summary>
    /// Gets all answer option values for given ids
    /// </summary>
    private List<AnswerDto> GetAnswerOptions(DataTable answerOptionsTable, string[] answerOptionIds)
    {
        var answerOptions = answerOptionsTable.AsEnumerable()
            .Where(row => answerOptionIds.Contains(row.Field<string>(AnswerOptionsColumns.OptionId)))
            .Select(row => new AnswerDto
            {
                AnswerId = row.Field<string>(AnswerOptionsColumns.OptionId)!,
                AnswerText = row.Field<string>(AnswerOptionsColumns.OptionText) ?? string.Empty,
                VersionId = _questionSet.Version.VersionId,
            })
            .ToList();

        return answerOptions;
    }

    /// <summary>
    /// Gets all rules attached to a given question
    /// </summary>
    private List<RuleDto> GetRules(DataTable rulesTable, string questionId)
    {
        var groupedRules = rulesTable
            .AsEnumerable()
            .Where(row => row.Field<string>(RulesColumns.QuestionId) == questionId)
            .GroupBy(row => Convert.ToInt32(row[RulesColumns.RuleId]));

        var rules = groupedRules
            .Select(group => new RuleDto
            {
                QuestionId = group.First().Field<string>(RulesColumns.QuestionId) ?? string.Empty,
                Sequence = Convert.ToInt32(group.First()[RulesColumns.Sequence]),
                ParentQuestionId = group.First().Field<string>(RulesColumns.ParentQuestionId),
                Mode = group.First().Field<string>(RulesColumns.Mode) ?? string.Empty,
                Description = group.First().Field<string>(RulesColumns.Description) ?? string.Empty,
                VersionId = _questionSet.Version.VersionId,
                Conditions = group
                    .Select(condition => new ConditionDto
                    {
                        Mode = condition.Field<string>(RulesColumns.ConditionMode) ?? string.Empty,
                        Operator = condition.Field<string>(RulesColumns.ConditionOperator) ?? string.Empty,
                        Value = condition.Field<string>(RulesColumns.ConditionValue),
                        Negate = condition.Field<bool>(RulesColumns.ConditionNegate),
                        ParentOptions = condition.Field<string>(RulesColumns.ConditionParentOptions)?.Split(",").ToList() ?? [],
                        OptionType = condition.Field<string>(RulesColumns.ConditionOptionType) ?? string.Empty,
                        Description = condition.Field<string>(RulesColumns.ConditionDescription),
                        IsApplicable = true,
                    })
                    .ToList()
            })
            .ToList();

        return rules;
    }
}