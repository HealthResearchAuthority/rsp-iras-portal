using Mapster;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Helpers;

public static class QuestionsetHelpers
{
    // Pseudocode:
    // - Convert CMS response to a flat list of QuestionsResponse.
    // - Order questions by SectionId then by Sequence to maintain desired order.
    // - Group by SectionId so we can assign an index that resets per section.
    // - Within each group, use Select with index to create (question, indexPerSection) tuples.
    // - Build QuestionnaireViewModel and add QuestionViewModel items using the per-section index.

    public static QuestionnaireViewModel BuildQuestionnaireViewModel(CmsQuestionSetResponse response, bool resetIndexPerSection = false)
    {
        var model = ConvertToQuestionResponse(response);

        IEnumerable<(QuestionsResponse, int)> questions = resetIndexPerSection ?
            // reset index for each unique SectionId
            model
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.SectionSequence)
                .ThenBy(q => q.Sequence)
                .GroupBy(q => q.SectionId)
                .SelectMany(g => g.Select((question, index) => (question, index))) :

            // order the questions by SectionId and Sequence
            model
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.SectionSequence)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

        var firstSection = response?.Sections?.FirstOrDefault();
        var guidanceContent = firstSection?.GuidanceComponents?.ToList() ?? [];

        var questionnaire = new QuestionnaireViewModel
        {
            GuidanceContent = guidanceContent
        };

        // build the questionnaire view model with per-section index
        foreach (var (question, index) in questions)
        {
            questionnaire.Questions.Add(new QuestionViewModel
            {
                Index = index,
                VersionId = question.VersionId,
                QuestionId = question.QuestionId,
                Category = question.Category,
                SectionId = question.SectionId,
                Section = question.Section,
                Sequence = question.Sequence,
                Heading = question.Heading,
                QuestionText = question.QuestionText,
                ShortQuestionText = question.ShortQuestionText,
                QuestionType = question.QuestionType,
                DataType = question.DataType,
                IsOptional = question.IsOptional,
                Rules = question.Rules,
                IsMandatory = question.IsMandatory,
                ShowOriginalAnswer = question.ShowOriginalAnswer,
                SectionSequence = question.SectionSequence,
                GuidanceComponents = question.GuidanceComponents,
                Answers = [.. question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                })]
            });
        }

        return questionnaire;
    }

    public static IEnumerable<QuestionsResponse> QuestionTransformer(SectionModel section)
    {
        var response = new List<QuestionsResponse>();
        foreach (var (question, i) in section.Questions.Select((value, i) => (value, i)))
        {
            var questionTransformed = new QuestionsResponse
            {
                IsMandatory = (question.Conformance == "Mandatory"),
                Heading = (i + 1).ToString(),
                Sequence = question.Sequence == 0 ? i + 1 : question.Sequence,
                Section = section.SectionName ?? string.Empty,
                SectionId = section.Id,
                QuestionId = question.Id,
                QuestionText = question.Name ?? string.Empty,
                ShortQuestionText = question.ShortName ?? string.Empty,
                DataType = question.AnswerDataType ?? string.Empty,
                QuestionType = question.QuestionFormat ?? string.Empty,
                Category = question.CategoryId ?? string.Empty,
                ShowOriginalAnswer = question.ShowOriginalAnswer,
                SectionSequence = question.SectionSequence,
                Answers = [],
                Rules = [],
                GuidanceComponents = question.GuidanceComponents,
                VersionId = question.Version ?? string.Empty
            };

            if (question.Answers != null && question.Answers.Any())
            {
                foreach (var answer in question.Answers)
                {
                    questionTransformed.Answers.Add(new AnswerDto
                    {
                        AnswerId = answer.Id ?? string.Empty,
                        AnswerText = answer.OptionName ?? string.Empty,
                    });
                }
            }

            if (question.ValidationRules != null && question.ValidationRules.Any())
            {
                foreach (var (validationRule, y) in question.ValidationRules.Select((value, y) => (value, y)))
                {
                    var ruleConditions = new List<ConditionDto>();
                    var transformedRule = validationRule.Adapt<RuleDto>();
                    transformedRule.Sequence = y;
                    transformedRule.ParentQuestionId = validationRule.ParentQuestion?.Id?.ToString();

                    foreach (var condition in validationRule.Conditions)
                    {
                        var conditionModel = condition.Adapt<ConditionDto>();
                        conditionModel.IsApplicable = true;
                        if (condition.ParentOptions != null)
                        {
                            conditionModel.ParentOptions = condition.ParentOptions.Select(x => x.Id.ToString()).ToList();
                        }

                        ruleConditions.Add(conditionModel);
                    }

                    transformedRule.Conditions = ruleConditions;
                    questionTransformed.Rules.Add(transformedRule);
                }
            }

            response.Add(questionTransformed);
        }

        return response;
    }

    public static List<QuestionsResponse> ConvertToQuestionResponse(CmsQuestionSetResponse response)
    {
        var model = new List<QuestionsResponse>();
        foreach (var section in response.Sections)
        {
            if (section.Questions != null && section.Questions.Any())
            {
                var mappedQUestion = QuestionTransformer(section);
                model.AddRange(mappedQUestion);
            }
        }
        return model;
    }
}