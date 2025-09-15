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
}