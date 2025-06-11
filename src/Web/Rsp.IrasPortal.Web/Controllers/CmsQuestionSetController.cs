using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = "IsUser")]
public class CmsQuestionSetController(ICmsQuestionSetServiceClient questionSetService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid questionSetId)
    {
        var model = new List<QuestionsResponse>();
        var questionSet = await questionSetService.GetQuestionSet(questionSetId.ToString());

        if (questionSet.IsSuccessful)
        {
            var questionsObject = questionSet.Content;

            foreach (var section in questionsObject.Sections)
            {
                if (section.Questions != null && section.Questions.Any())
                {
                    var mappedQUestion = QuestionTransformer(section);
                    model.AddRange(mappedQUestion);
                }
            }
        }

        var viewModelData = BuildQuestionnaireViewModel(model);
        viewModelData.CurrentStage = viewModelData.Questions.FirstOrDefault().SectionId;
        TempData[TempDataKeys.CurrentStage] = viewModelData.Questions.FirstOrDefault().SectionId;

        return View(viewModelData);
    }

    private IEnumerable<QuestionsResponse> QuestionTransformer(SectionModel section)
    {
        var response = new List<QuestionsResponse>();
        foreach (var (question, i) in section.Questions.Select((value, i) => (value, i)))
        {
            var questionTransformed = new QuestionsResponse
            {
                Heading = (i + 1).ToString(),
                Sequence = i + 1,
                Section = section.SectionName,
                SectionId = section.Id,
                QuestionId = question.Id,
                QuestionText = question.Name,
                IsMandatory = question.IsMandatory,
                IsOptional = !question.IsMandatory,
                DataType = question.AnswerDataType,
                QuestionType = question.QuestionFormat,
                Category = section.SectionName,
                Answers = new List<AnswerDto>(),
                Rules = new List<RuleDto>()
            };

            if (question.Answers != null && question.Answers.Any())
            {
                foreach (var answer in question.Answers)
                {
                    questionTransformed.Answers.Add(new AnswerDto
                    {
                        AnswerId = answer.Id,
                        AnswerText = answer.OptionName
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

    private static QuestionnaireViewModel BuildQuestionnaireViewModel(IEnumerable<QuestionsResponse> response)
    {
        // order the questions by SectionId and Sequence
        var questions = response
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

        var questionnaire = new QuestionnaireViewModel();

        // build the questionnaire view model
        // we need to order the questions by section and sequence
        // and also need to assign the index to the question so the multiple choice
        // answsers can be linked back to the question
        foreach (var (question, index) in questions)
        {
            questionnaire.Questions.Add(new QuestionViewModel
            {
                Index = index,
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
                IsMandatory = question.IsMandatory,
                IsOptional = question.IsOptional,
                Rules = question.Rules,
                Answers = question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                }).ToList()
            });
        }

        return questionnaire;
    }
}