using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications;

/// <summary>
/// Base controller for Modifications controllers, providing shared logic for
/// fetching a modification, preparing base view model, and retrieving
/// initial questions and modification changes.
/// </summary>
public abstract class ModificationsControllerBase
(
    IRespondentService respondentService,
    IProjectModificationsService projectModificationsService,
    ICmsQuestionsetService cmsQuestionsetService

) : Controller
{
    protected readonly IProjectModificationsService projectModificationsService = projectModificationsService;
    protected readonly ICmsQuestionsetService cmsQuestionsetService = cmsQuestionsetService;

    protected async Task<(IActionResult?, ModificationDetailsViewModel?)> GetModificationDetails(Guid projectModificationId, string irasId, string shortTitle, string projectRecordId)
    {
        // Fetch the modification by its identifier
        var modificationResponse = await projectModificationsService.GetModificationsByIds([projectModificationId.ToString()]);

        // Short-circuit with a service error if the call failed
        if (!modificationResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(modificationResponse), null);
        }

        if (modificationResponse.Content?.Modifications.Any() is false)
        {
            return (this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"Error retrieving the modification for project record: {projectRecordId} modificationId: {projectModificationId}",
            }), null);
        }

        // Select the first (and only) modification result
        var modification = modificationResponse.Content!.Modifications.First();

        // Build the base view model with project metadata
        return (null, new ModificationDetailsViewModel
        {
            ModificationId = modification.Id,
            IrasId = irasId,
            ShortTitle = shortTitle,
            ModificationIdentifier = modification.ModificationId,
            Status = modification.Status,
            ProjectRecordId = projectRecordId
        });
    }

    /// <summary>
    /// Builds the common header data needed by both ModificationDetails and ReviewAllChanges.
    /// </summary>
    protected async Task<(IActionResult?, StartingQuestionsDto? InitialQuestions, IEnumerable<ProjectModificationChangeResponse>? ModificationChanges)> GetModificationChanges
    (
        ModificationDetailsViewModel modification
    )
    {
        // Retrieve all changes related to this modification
        var modificationsResponse = await projectModificationsService.GetModificationChanges(Guid.Parse(modification.ModificationId!));

        if (!modificationsResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(modificationsResponse), default, default);
        }

        // Load initial questions to resolve display names for areas of change
        var initialQuestionsResponse = await cmsQuestionsetService.GetInitialModificationQuestions();

        if (!initialQuestionsResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(initialQuestionsResponse), default, default);
        }

        var initialQuestions = initialQuestionsResponse.Content!;

        // modification changes returned from the service
        var modificationChanges = modificationsResponse.Content!;

        return (null, initialQuestions, modificationChanges);
    }

    protected async Task SaveModificationAnswers(Guid projectModificationId, string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationId = projectModificationId,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId
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
            request.ModificationAnswers.Add(new RespondentAnswerDto
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
        if (request.ModificationAnswers.Count > 0)
        {
            await respondentService.SaveModificationAnswers(request);
        }
    }
}