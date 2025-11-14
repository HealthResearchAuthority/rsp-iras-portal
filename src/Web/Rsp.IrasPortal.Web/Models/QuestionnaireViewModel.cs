using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Web.Helpers;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel representing a questionnaire, including its questions and related state.
/// </summary>
public class QuestionnaireViewModel
{
    /// <summary>
    /// Indicates if the answers are being reviewed.
    /// </summary>
    public bool ReviewAnswers { get; set; }

    /// <summary>
    /// Indicates if the answers are being reviewed.
    /// </summary>
    public bool ReviewAllChanges { get; set; }

    /// <summary>
    /// The current stage of the questionnaire.
    /// </summary>
    public string? CurrentStage { get; set; } = "";

    /// <summary>
    /// List of questions in the questionnaire.
    /// </summary>
    public List<QuestionViewModel> Questions { get; set; } = [];

    public List<ComponentContent> GuidanceContent { get; set; } = [];

    /// <summary>
    /// ViewModel for searching and selecting a sponsor organisation.
    /// </summary>
    public OrganisationSearchViewModel SponsorOrgSearch { get; set; } = new();

    public Dictionary<string, RespondentAnswerDto> ProjectRecordAnswers { get; set; } = [];

    /// <summary>
    /// Gets a list of conditional rules for non-mandatory questions that have rules.
    /// </summary>
    /// <returns>List of objects containing QuestionId and Rules for applicable questions.</returns>
    public List<object> GetConditionalRules()
    {
        return [.. Questions
            .Where(q => !q.IsMandatory && q.Rules.Any())
            .Select(q => new
            {
                q.QuestionId,
                q.Rules
            })];
    }

    /// <summary>
    /// Gets a list of conditional rules for non-mandatory questions that have rules.
    /// </summary>
    /// <returns>List of objects containing QuestionType, DataType, AnswerText, SelectedOption, Answers for each parent question.</returns>
    public List<object> GetParentQuestionAnswers()
    {
        // Pseudocode:
        // 1. Create a set to hold unique parent question IDs.
        // 2. Iterate all questions:
        //    a. If question is non-mandatory and has rules:
        //       i. Add any non-null ParentQuestionId from its rules to the set.
        // 3. For each parent question ID:
        //    a. Find the matching question in Questions by QuestionId.
        //    b. If found, create an anonymous object with QuestionType, DataType, AnswerText, SelectedOption, Answers.
        // 4. Return the list of these objects.

        var parentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var question in Questions)
        {
            if (!question.IsMandatory && question.Rules.Any())
            {
                foreach (var rule in question.Rules)
                {
                    if (!string.IsNullOrWhiteSpace(rule.ParentQuestionId))
                    {
                        parentIds.Add(rule.ParentQuestionId!);
                    }
                }
            }
        }

        var results = new List<object>(parentIds.Count);

        foreach (var parentId in parentIds)
        {
            var parentQuestion = Questions
                .FirstOrDefault(q => q.QuestionId.Equals(parentId, StringComparison.OrdinalIgnoreCase));

            if (parentQuestion is null)
            {
                continue;
            }

            var data = new
            {
                parentQuestion.QuestionType,
                parentQuestion.DataType,
                parentQuestion.AnswerText,
                parentQuestion.SelectedOption,
                Answers = parentQuestion.Answers.Where(a => a.IsSelected)
            };

            results.Add(new
            {
                QuestionId = parentId,
                Answers = data
            });
        }

        return results;
    }

    /// <summary>
    /// Gets the answer text for the question with text "Short project title", if present.
    /// </summary>
    /// <returns>The answer text for the short project title, or null if not found.</returns>
    public string? GetShortProjectTitle()
    {
        return Questions
            .FirstOrDefault(q => q.QuestionId.Equals(QuestionIds.ShortProjectTitle, StringComparison.OrdinalIgnoreCase))?.AnswerText;
    }

    /// <summary>
    /// Gets the answer text for the question with QuestionId "IQA0003", which represents the planned end date of the project.
    /// </summary>
    /// <returns>
    /// The answer text for the planned end date of the project, or null if the question is not found.
    /// </returns>
    public string? GetProjectPlannedEndDate()
    {
        var plannedEndDate = Questions.FirstOrDefault(q => q.QuestionId.Equals(QuestionIds.ProjectPlannedEndDate, StringComparison.OrdinalIgnoreCase))?.AnswerText;
        return DateHelper.ConvertDateToString(plannedEndDate!);
    }

    /// <summary>
    /// Update Questions with answers provided in the model
    /// </summary>
    /// <param name="submittedModel">Model with the submitted answers</param>
    /// <param name="sourceModel">Model to be updated</param>
    public void UpdateWithAnswers(List<QuestionViewModel> submittedModel, List<QuestionViewModel>? sourceModel = null)
    {
        sourceModel ??= Questions;

        // update the model with the answers
        // provided by the applicant
        foreach (var question in sourceModel)
        {
            // find the question in the submitted model
            // that matches the index
            var response = submittedModel.Find(q => q.Index == question.Index);

            // update the question with provided answers
            question.SelectedOption = response?.SelectedOption;

            if (question.DataType != "Dropdown")
            {
                question.Answers = response?.Answers ?? [];
            }

            question.AnswerText = response?.AnswerText;
            // update the date fields if they are present
            question.Day = response?.Day;
            question.Month = response?.Month;
            question.Year = response?.Year;
        }
    }

    /// <summary>
    /// Updates the provided QuestionViewModel with RespondentAnswers
    /// </summary>
    /// <param name="respondentAnswers">Respondent Answers</param>
    public void UpdateWithRespondentAnswers(IEnumerable<RespondentAnswerDto> respondentAnswers, List<QuestionViewModel>? sourceModel = null)
    {
        sourceModel ??= Questions;

        foreach (var respondentAnswer in respondentAnswers)
        {
            // for each respondentAnswer find the question in the
            // questionviewmodel
            var question = sourceModel.Find(q => q.QuestionId == respondentAnswer.QuestionId)!;

            // continue to next question if we
            // don't have an answer
            if (question == null)
            {
                continue;
            }

            // set the selected option
            question.SelectedOption = respondentAnswer.SelectedOption;

            // if the question was multiple choice type i.e. checkboxes
            if (respondentAnswer.OptionType == "Multiple")
            {
                // set the IsSelected property to true
                // where the answerId matches with the respondent answer
                question.Answers.ForEach(ans =>
                {
                    var answer = respondentAnswer.Answers.Find(ra => ans.AnswerId == ra);
                    if (answer != null)
                    {
                        ans.IsSelected = true;
                    }
                });
            }

            // update the freetext answer
            question.AnswerText = respondentAnswer.AnswerText;
        }
    }
}