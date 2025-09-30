using System.Data;
using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using static Rsp.IrasPortal.Application.Constants.TempDataKeys;

namespace Rsp.IrasPortal.Web.Features.Modifications;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
//[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ModificationChangesBaseController
(
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
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
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationChangeAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId,
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
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    protected async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model, bool validateMandatory = false)
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

        if (validateMandatory)
        {
            context.RootContextData["ValidateMandatoryOnly"] = true;
        }

        // this is required to get the questions in the validator
        // before the validation cicks in
        context.RootContextData["questions"] = model.Questions;

        // call the ValidateAsync to execute the validation
        // this will trigger the fluentvalidation using the injected validator if configured
        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return false;
        }

        return true;
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
    protected async Task<NavigationDto> SetStage(string sectionId)
    {
        var specificAreaOfChangeId = GetSpecificAreaOfChangeId();

        if (specificAreaOfChangeId == Guid.Empty)
        {
            return new();
        }

        // Fetch previous, current, and next section responses from the question set service
        var currentResponse = await cmsQuestionsetService.GetModificationQuestionSet(sectionId);
        var previousResponse = await cmsQuestionsetService.GetModificationPreviousQuestionSection(sectionId);
        var nextResponse = await cmsQuestionsetService.GetModificationNextQuestionSection(sectionId);

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

        // get the questions for all categories
        var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        // return the error view if unsuccessfull
        if (!questionSetServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(questionSetServiceResponse);
        }

        if (reviseChange)
        {
            var questionSetResponse = questionSetServiceResponse.Content!;

            // find the ReviewChanges section
            var section = questionSetResponse
                .Sections
                .Single(s => s.StaticViewName?.Equals(nameof(ReviewChanges), StringComparison.OrdinalIgnoreCase) is true);

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
                 select specificArea.OptionName).SingleOrDefault();

            if (!string.IsNullOrWhiteSpace(specificAreaOfChange))
            {
                TempData[ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange;
            }

            TempData[ProjectModification.SpecificAreaOfChangeId] = specificAreaOfChangeId;
            TempData[ProjectModification.ProjectModificationChangeId] = modificationChangeId;

            await SetStage(section.SectionId);
        }

        var respondentAnswers = respondentServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);

        var questions = questionnaire.Questions;

        // validate each category
        foreach (var questionsGroup in questions.ToLookup(x => x.Category))
        {
            var category = questionsGroup.Key;

            // get the answers for the category
            var answers = respondentAnswers.Where(r => r.CategoryId == category).ToList();

            if (answers.Count > 0)
            {
                // if we have answers, update the model with the provided answers
                ModificationHelpers.UpdateWithAnswers(respondentAnswers, [.. questionsGroup.Select(q => q)]);
            }

            if (reviseChange)
            {
                // using the FluentValidation, create a new context for the model
                var context = new ValidationContext<QuestionnaireViewModel>(questionnaire);

                // this is required to get the questions in the validator
                // before the validation cicks in
                context.RootContextData["questions"] = questionnaire.Questions;
                context.RootContextData["ValidateMandatoryOnly"] = true;

                // call the ValidateAsync to execute the validation
                // this will trigger the fluentvalidation using the injected validator if configured
                var result = await validator.ValidateAsync(context);
                if (!result.IsValid)
                {
                    // Copy the validation results into ModelState.
                    // ASP.NET uses the ModelState collection to populate
                    // error messages in the View.
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }

                    return View("ModificationChangesReview", questionnaire);
                }
            }
        }

        return View("ModificationChangesReview", questionnaire);
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    public async Task<IActionResult> ConfirmModificationChanges()
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        // get the application from the session
        // to get the projectApplicationId
        var projectRecordId = TempData.Peek(ProjectRecordId) as string ?? string.Empty;

        // get the respondent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationChangeAnswers(projectModificationChangeId, projectRecordId);

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        var specificAreaOfChangeId = GetSpecificAreaOfChangeId();

        // get the questions for all categories
        var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

        // return the error view if unsuccessfull
        if (!questionSetServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(questionSetServiceResponse);
        }

        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);
        var questions = questionnaire.Questions;

        var respondentAnswers = respondentServiceResponse.Content!;

        // validate each category
        foreach (var questionsGroup in questions.ToLookup(q => q.Category))
        {
            var category = questionsGroup.Key;

            // get the answers for the category
            var answers = respondentAnswers.Where(r => r.CategoryId == category).ToList();

            ValidationContext<QuestionnaireViewModel> context;

            // if we have answers, update the model with the provided answers
            ModificationHelpers.UpdateWithAnswers(answers, [.. questionsGroup.Select(q => q)]);

            #region orginal validation

            //// using the FluentValidation, create a new context for the model
            //context = new ValidationContext<QuestionnaireViewModel>(questionnaire);

            //// this is required to get the questions in the validator
            //// before the validation cicks in
            //context.RootContextData["questions"] = questionnaire.Questions;
            //context.RootContextData["ValidateMandatoryOnly"] = true;

            //// call the ValidateAsync to execute the validation
            //// this will trigger the fluentvalidation using the injected validator if configured
            //var result = await validator.ValidateAsync(context);
            //if (!result.IsValid)
            //{
            //    // Copy the validation results into ModelState.
            //    // ASP.NET uses the ModelState collection to populate
            //    // error messages in the View.
            //    foreach (var error in result.Errors)
            //    {
            //        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            //    }

            //    return View("ModificationChangesReview", questionnaire);
            //}

            #endregion orginal validation
        }

        // change it to go to the confirmation page i.e. submitted to sponsor
        // no next stage so redirect to modification details page
        var irasId = TempData.Peek(IrasId) as int?;
        var shortTitle = TempData.Peek(ShortProjectTitle) as string;

        return RedirectToRoute("pmc:modificationdetails", new { projectRecordId, irasId, shortTitle, projectModificationId });
    }
}