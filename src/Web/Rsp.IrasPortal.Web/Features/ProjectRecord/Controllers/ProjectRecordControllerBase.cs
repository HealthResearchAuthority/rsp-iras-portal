using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.ProjectRecord.Controllers;

public abstract class ProjectRecordControllerBase
(
    IRespondentService respondentService
) : Controller
{
    protected async Task SaveProjectRecordAnswers(string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new RespondentAnswersRequest
        {
            ProjectRecordId = projectRecordId,
            Id = respondentId
        };

        foreach (var question in questions)
        {
            // we need to identify if it's a
            // multiple choice or a single choice question
            // this is to determine if the responses
            // should be saved as comma seprated values
            // or a single value
            var optionType = question.DataType switch
            {
                "Boolean" or "Radio button" or "Look-up list" or "Dropdown" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            // build RespondentAnswers model
            request.RespondentAnswers.Add(new RespondentAnswerDto
            {
                QuestionId = question.QuestionId,
                VersionId = question.VersionId ?? string.Empty,
                AnswerText = question.AnswerText,
                CategoryId = question.Category,
                SectionId = question.SectionId,
                SelectedOption = question.SelectedOption,
                OptionType = optionType,
                Answers = question.Answers
                                .Where(a => a.IsSelected)
                                .Select(ans => ans.AnswerId)
                                .ToList()
            });
        }

        // if user has answered some or all of the questions
        // call the api to save the responses
        if (request.RespondentAnswers.Count > 0)
        {
            await respondentService.SaveRespondentAnswers(request);
        }
    }
}