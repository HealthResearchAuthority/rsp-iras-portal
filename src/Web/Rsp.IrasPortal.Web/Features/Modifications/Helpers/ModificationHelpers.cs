using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Helpers;

public static class ModificationHelpers
{
    /// <summary>
    /// Updates the provided QuestionViewModel with RespondentAnswers
    /// </summary>
    /// <param name="respondentAnswers">Respondent Answers</param>
    /// <param name="questionAnswers">QuestionViewModel with answers</param>
    public static void UpdateWithAnswers(IEnumerable<RespondentAnswerDto> respondentAnswers, List<QuestionViewModel> questionAnswers)
    {
        foreach (var respondentAnswer in respondentAnswers)
        {
            // for each respondentAnswer find the question in the
            // questionviewmodel
            var question = questionAnswers.Find(q => q.QuestionId == respondentAnswer.QuestionId)!;

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

    /// <summary>
    /// Applies respondent answers to a questionnaire and trims conditional/unanswered questions.
    /// Also returns the surfacing question (if any) and whether it should be shown for the provided action name.
    /// </summary>
    /// <param name="questionnaire">Questionnaire view model to update</param>
    /// <param name="respondentAnswers">Respondent answers to apply</param>
    /// <param name="actionName">Optional action name used to determine surfacing question behaviour</param>
    /// <returns>Tuple of (surfacingQuestion, showSurfacingQuestion)</returns>
    public static (QuestionViewModel? surfacingQuestion, bool showSurfacingQuestion) ApplyRespondentAnswersAndTrim(QuestionnaireViewModel questionnaire, IEnumerable<RespondentAnswerDto> respondentAnswers, string? actionName = null)
    {
        var questions = questionnaire.Questions;

        // validate each category and update answers where present
        foreach (var questionsGroup in questions.ToLookup(x => x.Category))
        {
            var category = questionsGroup.Key;

            // get the answers for the category
            var answers = respondentAnswers.Where(r => r.CategoryId == category).ToList();

            if (answers.Count > 0)
            {
                // if we have answers, update the model with the provided answers
                UpdateWithAnswers(answers, questionsGroup.Select(q => q).ToList());
            }
        }

        // get the question to check if the answer to the question should be shown
        var surfacingQuestion = questions.Find(q => !string.IsNullOrWhiteSpace(q.ShowAnswerOn));
        var showSurfacingQuestion = surfacingQuestion?.ShowAnswerOn.Contains(actionName ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;

        // remove the question from the list if we are showing the surfacing question
        if (showSurfacingQuestion)
        {
            questions.RemoveAll(q => q.QuestionId == surfacingQuestion!.QuestionId);
        }

        // remove all the conditional questions without answers, these must have been
        // validated on the previous screen
        questions.RemoveAll(q => !(q.IsMandatory || q.IsOptional) && q.IsMissingAnswer());

        return (surfacingQuestion, showSurfacingQuestion);
    }
}