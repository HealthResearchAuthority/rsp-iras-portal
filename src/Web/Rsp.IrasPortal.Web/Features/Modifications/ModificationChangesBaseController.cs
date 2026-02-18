using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;
using static Rsp.Portal.Application.Constants.TempDataKeys;

namespace Rsp.Portal.Web.Features.Modifications;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
public class ModificationChangesBaseController
(
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IModificationRankingService modificationRankingService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    protected (Guid ModificationId, Guid ModificationChangeId) CheckModification()
    {
        // check if we are in the modification journey, so only get the modfication questions
        var modificationId = TempData.Peek(ProjectModification.ProjectModificationId);
        var modificationChangeId = TempData.Peek(ProjectModification.ProjectModificationChangeId);

        var modification = (Guid.Empty, Guid.Empty);

        if (modificationId is not null)
        {
            modification.Item1 = (Guid)modificationId;
        }

        if (modificationChangeId is not null)
        {
            modification.Item2 = (Guid)modificationChangeId;
        }

        return modification;
    }

    protected async Task SaveModificationChangeAnswers(Guid projectModificationChangeId, string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var userId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationChangeAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId,
            ProjectRecordId = projectRecordId,
            UserId = userId
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
            request.ModificationChangeAnswers.Add(new RespondentAnswerDto
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
        if (request.ModificationChangeAnswers.Count > 0)
        {
            await respondentService.SaveModificationChangeAnswers(request);
        }
    }

    /// <summary>
    /// Sets the Previous, Current, and Next stages required for navigation.
    /// </summary>
    /// <param name="sectionId">Section Id of the current stage</param>
    /// <returns>A <see cref="NavigationDto"/> containing previous, current, and next stage/category information for navigation.</returns>
    /// <remarks>
    /// This method retrieves the previous, current, and next question sections for the given section and category.
    /// It uses the QuestionSetService to fetch section details and stores navigation state in TempData for use in the UI.
    /// </remarks>
    protected async Task<NavigationDto> SetStage(string sectionId, string? parentQuestionId = null, string? parentAnswerOption = null)
    {
        var specificAreaOfChangeId = GetSpecificAreaOfChangeId();

        if (specificAreaOfChangeId == Guid.Empty)
        {
            return new();
        }

        // Fetch previous, current, and next section responses from the question set service
        var currentResponse = await cmsQuestionsetService.GetModificationQuestionSet(sectionId);
        var previousResponse = await cmsQuestionsetService.GetModificationPreviousQuestionSection(sectionId, parentQuestionId, parentAnswerOption);
        var nextResponse = await cmsQuestionsetService.GetModificationNextQuestionSection(sectionId, parentQuestionId, parentAnswerOption);

        // Extract previous stage and category if available and matches the current category
        string previousStage = (previousResponse.IsSuccessStatusCode, previousResponse.Content?.SectionId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => previousResponse.Content.SectionId,
            _ => string.Empty
        };

        string previousCategory = (previousResponse.IsSuccessStatusCode, previousResponse.Content?.QuestionCategoryId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => previousResponse.Content.QuestionCategoryId,
            _ => string.Empty
        };

        // Find the current section in the list of all sections for the current category
        var currentSection = currentResponse?.Content?.Sections.FirstOrDefault(section => section.Id == sectionId);
        string currentStage = currentSection?.SectionId ?? sectionId;
        string currentCategory = currentSection?.CategoryId ?? "";

        // Extract next stage and category if available and matches the current category
        string nextStage = (nextResponse.IsSuccessStatusCode, nextResponse.Content?.SectionId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => nextResponse.Content.SectionId,
            _ => string.Empty
        };

        string nextCategory = (nextResponse.IsSuccessStatusCode, nextResponse.Content?.QuestionCategoryId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => nextResponse.Content.QuestionCategoryId,
            _ => string.Empty
        };

        // Store navigation state in TempData for use in the UI
        TempData[PreviousStage] = previousStage;
        TempData[PreviousCategory] = previousCategory;
        TempData[CurrentStage] = currentStage;

        // Return the navigation DTO with all relevant navigation information
        var navigationDto = new NavigationDto
        {
            PreviousCategory = previousCategory,
            PreviousStage = previousStage,
            CurrentCategory = currentCategory,
            CurrentStage = currentStage,
            NextCategory = nextCategory,
            NextStage = nextStage,
            PreviousSection = previousResponse.Content
        };

        if (currentSection != null)
        {
            navigationDto.CurrentSection = currentSection.Adapt<QuestionSectionsResponse>();
        }

        navigationDto.NextSection = nextResponse.Content;

        TempData[ProjectModificationChange.Navigation] = JsonSerializer.Serialize(navigationDto);

        return navigationDto;
    }

    protected Guid GetSpecificAreaOfChangeId()
    {
        var specificAreaOfChange = TempData.Peek(ProjectModification.SpecificAreaOfChangeId);

        return specificAreaOfChange is not null ?
            (Guid)specificAreaOfChange :
            Guid.Empty;
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    /// <param name="projectRecordId">ApplicationId to submit</param>
    public async Task<IActionResult> ReviewChanges(string projectRecordId, Guid specificAreaOfChangeId, Guid modificationChangeId, bool reviseChange = false)
    {
        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationChangeAnswers(modificationChangeId, projectRecordId);

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        // get the questions for all categories
        var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

        // return the error view if unsuccessfull
        if (!questionSetServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(questionSetServiceResponse);
        }

        // Load initial questions to resolve display names for areas of change
        var initialQuestionsResponse = await cmsQuestionsetService.GetInitialModificationQuestions();

        if (!initialQuestionsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(initialQuestionsResponse);
        }

        var initialQuestions = initialQuestionsResponse.Content!;

        var specificAreaOfChange =
            (from area in initialQuestions.AreasOfChange
             let specificAreas = area.SpecificAreasOfChange
             from specificArea in specificAreas
             where specificArea.AutoGeneratedId == specificAreaOfChangeId.ToString()
             select specificArea).SingleOrDefault();

        if (reviseChange)
        {
            var questionSetResponse = questionSetServiceResponse.Content!;

            // find the ReviewChanges section
            var section = questionSetResponse
                .Sections
                .Single(s => s.StaticViewName?.Equals(nameof(ReviewChanges), StringComparison.OrdinalIgnoreCase) is true);

            if (!string.IsNullOrWhiteSpace(specificAreaOfChange?.OptionName))
            {
                TempData[ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange.OptionName;
            }

            TempData[ProjectModification.SpecificAreaOfChangeId] = specificAreaOfChangeId;
            TempData[ProjectModification.ProjectModificationChangeId] = modificationChangeId;

            await SetStage(section.SectionId);
        }

        TempData[ProjectModification.ShowApplicabilityQuestions] = specificAreaOfChange?.ShowApplicabilityQuestions;

        var respondentAnswers = respondentServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);

        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        if (reviseChange)
        {
            var isValid = await this.ValidateQuestionnaire(validator, questionnaire, true);

            if (!isValid)
            {
                return View("ModificationChangesReview", questionnaire);
            }
        }

        return View("ModificationChangesReview", questionnaire);
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    [ExcludeFromCodeCoverage]
    public async Task<IActionResult> ConfirmModificationChanges()
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();
        var projectRecordId = TempData.Peek(ProjectRecordId) as string ?? string.Empty;

        await modificationRankingService.UpdateChangeRanking(projectModificationChangeId, projectRecordId);
        await modificationRankingService.UpdateOverallRanking(projectModificationId, projectRecordId);

        var irasId = TempData.Peek(IrasId);
        var shortTitle = TempData.Peek(ShortProjectTitle) as string;
        var sponsorOrganisationUserIdStr = TempData.Peek(RevisionSponsorOrganisationUserId)?.ToString();
        var rtsId = TempData.Peek(RevisionRtsId) as string;

        var routeValues = new RouteValueDictionary
        {
            ["projectRecordId"] = projectRecordId,
            ["irasId"] = irasId,
            ["shortTitle"] = shortTitle,
            ["projectModificationId"] = projectModificationId,
        };

        if (!string.IsNullOrWhiteSpace(sponsorOrganisationUserIdStr))
        {
            routeValues["sponsorOrganisationUserId"] = sponsorOrganisationUserIdStr;
            routeValues["rtsId"] = rtsId;
        }

        return RedirectToRoute("pmc:modificationdetails", routeValues);
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> SaveForLater(string projectRecordId, string routeName, string? sponsorOrganisationUserId = null, string? rtsId = null)
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        await modificationRankingService.UpdateChangeRanking(projectModificationChangeId, projectRecordId);
        await modificationRankingService.UpdateOverallRanking(projectModificationId, projectRecordId);

        TempData[ShowNotificationBanner] = true;
        TempData[ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

        var routeValues = new RouteValueDictionary
        {
            ["projectRecordId"] = projectRecordId
        };

        if (!string.IsNullOrWhiteSpace(sponsorOrganisationUserId))
        {
            routeValues["SponsorOrganisationUserId"] = sponsorOrganisationUserId;
        }

        if (!string.IsNullOrWhiteSpace(rtsId))
        {
            routeValues["rtsId"] = rtsId;
        }

        return RedirectToRoute(routeName, routeValues);
    }
}