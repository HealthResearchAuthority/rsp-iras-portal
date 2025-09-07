using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "mqc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ModificationQuestionnaireController
(
    IApplicationsService applicationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService questionSetService,
    IRtsService rtsService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    // as the controller name is different, but all views are under ProjectModification
    // folder, we need to provide the path to the specific view as they won't be automatically
    // discovered using the default conventions.

    private const string Index = "Views/ProjectModification/Index.cshtml";

    private const string ModificationChangesReview = "Views/ProjectModification/ModificationChangesReview.cshtml";

    private const string PostApprovalRoute = "pov:postapproval";

    /// <summary>
    /// Resumes the application for the categoryId
    /// </summary>
    /// <param name="projectRecordId">Application Id</param>
    /// <param name="categoryId">CategoryId to resume from</param>
    public async Task<IActionResult> Resume(string projectRecordId, string categoryId, string? sectionId = null)
    {
        // load existing application in session
        if (await LoadApplication(projectRecordId) == null)
        {
            return NotFound();
        }

        // check if we are in the modification journey, so only get the modfication questions
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        if (projectModificationChangeId == Guid.Empty)
        {
            return this.ServiceError(new ServiceResponse
            {
                Error = "Modification change not found",
                ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                StatusCode = HttpStatusCode.BadRequest
            });
        }

        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationAnswers(projectModificationChangeId, projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        var sectionIdOrDefault = sectionId ?? string.Empty;
        var questionsSetServiceResponse = await questionSetService.GetModificationQuestionSet(sectionIdOrDefault);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the respondent answers and questions
        var respondentAnswers = respondentServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!);

        var questions = questionnaire.Questions;

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            UpdateWithAnswers(respondentAnswers, questions);
        }

        // save the list of QuestionViewModel in session to get it later
        TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{sectionId}"] = JsonSerializer.Serialize(questions);

        // this is where the questionnaire will resume
        var navigationDto = await SetStage(sectionIdOrDefault);

        questionnaire.CurrentStage = navigationDto.CurrentStage;

        // continue to resume for the category Id &
        return RedirectToAction(nameof(DisplayQuestionnaire), new
        {
            sectionId
        });
    }

    /// <summary>
    /// Renders all of the questions for the categoryId
    /// </summary>
    ///<param name="sectionId">sectionId of the questions to be rendered</param>
    public async Task<IActionResult> DisplayQuestionnaire(string sectionId, bool reviewAnswers = false)
    {
        var questions = default(List<QuestionViewModel>);

        // get the existing questionnaire for the category if exists in the session
        if (TempData.ContainsKey($"{TempDataKeys.ProjectModification.Questionnaire}:{sectionId}"))
        {
            var questionsJson = TempData.Peek($"{TempDataKeys.ProjectModification.Questionnaire}:{sectionId}") as string;

            // get the questionnaire from the session
            // and deserialize it
            questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(questionsJson!)!;
        }

        // questionnaire doesn't exist in session so
        // get questions from the database for the category
        if (questions == null || questions.Count == 0)
        {
            var response = await questionSetService.GetModificationQuestionSet(sectionId: sectionId);

            if (!response.IsSuccessStatusCode)
            {
                // return error page as api wasn't successful
                return this.ServiceError(response);
            }

            // set the active stage for the category
            await SetStage(sectionId);

            // convert the questions response to QuestionnaireViewModel
            var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(response.Content!);

            // store the questions to load again if there are validation errors on the page
            TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{sectionId}"] = JsonSerializer.Serialize(questionnaire.Questions);

            return View(Index, questionnaire);
        }

        // set the active stage for the category
        await SetStage(sectionId);

        // if we have questions in the session
        // then return the view with the model
        return View(Index, new QuestionnaireViewModel
        {
            CurrentStage = sectionId,
            Questions = questions,
            ReviewAnswers = reviewAnswers
        });
    }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, string searchedPerformed, bool autoSearchEnabled, string saveAndContinue = "False", string saveForLater = "False")
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        var modificationQuestionnaire = TempData.Peek($"{TempDataKeys.ProjectModification.Questionnaire}:{model.CurrentStage}") as string;

        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(modificationQuestionnaire!)!;

        // update the model with the answeres
        // provided by the applicant
        foreach (var question in questions)
        {
            // find the question in the submitted model
            // that matches the index
            var response = model.Questions.Find(q => q.Index == question.Index);

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

        if (!autoSearchEnabled)
        {
            var sponsorOrgInput = questions.FirstOrDefault(q => string.Equals(q.QuestionType, "rts:org_lookup", StringComparison.OrdinalIgnoreCase));

            // Check if the sponsor organisation input is null or empty.
            if (sponsorOrgInput is not null)
            {
                var searchPerformed = searchedPerformed == "searched:true";
                var selectedOrg = model.SponsorOrgSearch.SelectedOrganisation;
                var searchedText = model.SponsorOrgSearch.SearchText;

                if (searchPerformed)
                {
                    // If a search was performed, only assign if a selection was made
                    sponsorOrgInput.AnswerText = string.IsNullOrWhiteSpace(selectedOrg) ? string.Empty : selectedOrg;
                }
                else
                {
                    // No search was performed, check if we have a selected org previously
                    if (!string.IsNullOrWhiteSpace(selectedOrg))
                    {
                        // searched text was cleared or changed
                        if
                        (
                            string.IsNullOrWhiteSpace(searchedText) ||
                            !selectedOrg.Equals(searchedText, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            // Cleared search or mismatch
                            sponsorOrgInput.AnswerText = string.Empty;
                            model.SponsorOrgSearch.SelectedOrganisation = string.Empty;
                        }
                        else
                        {
                            sponsorOrgInput.AnswerText = selectedOrg;
                        }
                    }
                    else
                    {
                        sponsorOrgInput.AnswerText = string.Empty;
                    }
                }
            }
        }

        // override the submitted model
        // with the updated model with answers
        model.Questions = questions;

        // At this point only validating the data format like date, email, length etc if provided
        // so that users can continue without entering the information. From the review screen
        // all mandatory questions will be validated
        var isValid = await ValidateQuestionnaire(model);

        if (!isValid)
        {
            // set the previous, current and next stages
            await SetStage(model.CurrentStage!);
            model.ReviewAnswers = false;
            return View(Index, model);
        }

        // ------------------Save Modification Answers-------------------------
        // get the application from the session
        // to get the projectApplicationId
        var application = (await LoadApplication())!;

        await SaveModificationAnswers(projectModificationChangeId, application.Id, questions);

        // set the previous, current and next stages
        var navigation = await SetStage(model.CurrentStage);

        // save the questions in the session
        TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{navigation.CurrentStage}"] = JsonSerializer.Serialize(questions);

        // user clicks on the SaveAndContinue button
        // so we need to resume from the next stage
        if (saveAndContinue == bool.TrueString)
        {
            // if the user is at the last stage and clicks on Save and Continue
            if (string.IsNullOrWhiteSpace(navigation.NextStage))
            {
                return RedirectToAction(nameof(SubmitApplication), new { projectRecordId = application.Id });
            }
            else
            {
                // TODO: Add logic for the following
                // or the next screen is dependent on the response of the current screen
                // if the answers are not provided, take them to ReviewPage
            }

            // otherwise resume from the NextStage in sequence
            return RedirectToAction(nameof(Resume), new
            {
                projectRecordId = application.Id,
                categoryId = navigation.NextCategory,
                sectionId = navigation.NextStage
            });
        }

        if (saveForLater == bool.TrueString)
        {
            return RedirectToRoute(PostApprovalRoute, new { projectRecordId = application.Id });
        }

        // continue rendering the questionnaire if the above conditions are not true
        return RedirectToAction(nameof(DisplayQuestionnaire), new
        {
            navigation.NextStage
        });
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    /// <param name="projectRecordId">ApplicationId to submit</param>
    public async Task<IActionResult> SubmitApplication(string projectRecordId)
    {
        var (projectModificationId, projectModificationChangeId) = CheckModification();

        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationAnswers(projectModificationChangeId, projectRecordId);

        var specificAreaOfChange = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);

        var specificAreaOfChangeId = Guid.Empty;

        if (specificAreaOfChange is not null)
        {
            specificAreaOfChangeId = (Guid)specificAreaOfChange;
        }

        // get the questions for all categories
        var questionSetServiceResponse = await questionSetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

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
                UpdateWithAnswers(respondentAnswers, [.. questionsGroup.Select(q => q)]);
            }
        }

        return View(ModificationChangesReview, questionnaire);
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
        var application = await LoadApplication();

        // get the respondent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationAnswers(projectModificationChangeId, application!.Id);

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        var specificAreaOfChange = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);

        var specificAreaOfChangeId = Guid.Empty;

        if (specificAreaOfChange is not null)
        {
            specificAreaOfChangeId = (Guid)specificAreaOfChange;
        }

        // get the questions for all categories
        var questionSetServiceResponse = await questionSetService.GetModificationsJourney(specificAreaOfChangeId.ToString());

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
            UpdateWithAnswers(answers, [.. questionsGroup.Select(q => q)]);

            // using the FluentValidation, create a new context for the model
            context = new ValidationContext<QuestionnaireViewModel>(questionnaire);

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

                return View(ModificationChangesReview, questionnaire);
            }
        }

        // TODO: When we have the requirements ready,
        // change it to go to the confirmation page i.e. submitted to sponsor
        return RedirectToRoute(PostApprovalRoute, new { projectRecordId = application.Id });
    }

    /// <summary>
    /// Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(QuestionnaireViewModel model, string? role, int? pageSize, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // override the submitted model
        // with the updated model with answers
        model.Questions = GetQuestionsFromSession(model);

        // set the previous, current and next stages
        await SetStage(model.CurrentStage!);

        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.SponsorOrgSearch.SelectedOrganisation = string.Empty;

        // save the list of QuestionViewModel in session to get it later
        TempData[$"{TempDataKeys.ProjectModification.Questionnaire}:{model.CurrentStage}"] = JsonSerializer.Serialize(model.Questions);

        // add the search model to temp data to use in the view
        TempData.TryAdd(TempDataKeys.OrgSearch, model.SponsorOrgSearch, true);

        if (string.IsNullOrEmpty(model.SponsorOrgSearch.SearchText) || model.SponsorOrgSearch.SearchText.Length < 3)
        {
            // add model validation error if search text is empty
            ModelState.AddModelError("sponsor_org_search", "Please provide 3 or more characters to search sponsor organisation.");

            // save the model state in temp data, to use it on redirects to show validation errors
            // the modelstate will be merged using the action filter ModelStateMergeAttribute
            // only if the TempData has ModelState stored
            TempData.TryAdd(TempDataKeys.ModelState, ModelState.ToDictionary(), true);

            // Return the view with the model state errors.
            return Redirect(returnUrl);
        }

        // Use the default sponsor role if no role is provided.
        role ??= OrganisationRoles.Sponsor;

        // Fetch organisations from the RTS service, with or without pagination.
        var searchResponse = await rtsService.GetOrganisationsByName(model.SponsorOrgSearch.SearchText, role, pageIndex, pageSize);

        // Handle error response from the service.
        if (!searchResponse.IsSuccessStatusCode || searchResponse.Content == null)
        {
            return this.ServiceError(searchResponse);
        }

        // Convert the response content to a list of organisation names.
        var sponsorOrganisations = searchResponse.Content;

        TempData.TryAdd(TempDataKeys.SponsorOrganisations, sponsorOrganisations, true);

        return Redirect(returnUrl);
    }

    private (Guid ModificationId, Guid ModificationChangeId) CheckModification()
    {
        // check if we are in the modification journey, so only get the modfication questions
        var modificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);
        var modificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);

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

    private async Task SaveModificationAnswers(Guid projectModificationChangeId, string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationAnswersRequest
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

    private List<QuestionViewModel> GetQuestionsFromSession(QuestionnaireViewModel model)
    {
        var questionsData = TempData.Peek($"{TempDataKeys.ProjectModification.Questionnaire}:{model.CurrentStage}") as string;

        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(questionsData!)!;

        // update the model with the answeres
        // provided by the applicant
        foreach (var question in questions)
        {
            // find the question in the submitted model
            // that matches the index
            var response = model.Questions.Find(q => q.Index == question.Index);

            // update the question with provided answers
            question.SelectedOption = response?.SelectedOption;
            question.Answers = response?.Answers ?? [];
            question.AnswerText = response?.AnswerText;
        }

        return questions;
    }

    /// <summary>
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model, bool validateMandatory = false)
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
    /// Updates the provided QuestionViewModel with RespondentAnswers
    /// </summary>
    /// <param name="respondentAnswers">Respondent Answers</param>
    /// <param name="questionAnswers">QuestionViewModel with answers</param>
    private static void UpdateWithAnswers(IEnumerable<RespondentAnswerDto> respondentAnswers, List<QuestionViewModel> questionAnswers)
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
    /// Loads the existing application from the database
    /// </summary>
    /// <param name="projectRecordId">Application Id</param>
    private async Task<IrasApplicationResponse?> LoadApplication(string? projectRecordId = null)
    {
        if (string.IsNullOrWhiteSpace(projectRecordId))
        {
            projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        }

        if (string.IsNullOrWhiteSpace(projectRecordId))
        {
            return null;
        }

        // get the application by id
        var response = await applicationsService.GetProjectRecord(projectRecordId);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var irasApplication = response.Content!;

        return irasApplication;
    }

    /// <summary>
    /// Sets the Previous, Current, and Next stages required for navigation.
    /// </summary>
    /// <param name="section">Section of the current stage</param>
    /// <returns>A <see cref="NavigationDto"/> containing previous, current, and next stage/category information for navigation.</returns>
    /// <remarks>
    /// This method retrieves the previous, current, and next question sections for the given section and category.
    /// It uses the QuestionSetService to fetch section details and stores navigation state in TempData for use in the UI.
    /// </remarks>
    private async Task<NavigationDto> SetStage(string section)
    {
        // Fetch previous, current, and next section responses from the question set service
        var previousResponse = await questionSetService.GetModificationPreviousQuestionSection(section);
        var currentResponse = await questionSetService.GetModificationQuestionSet();
        var nextResponse = await questionSetService.GetModificationNextQuestionSection(section);

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
        var currentSection = currentResponse?.Content?.Sections.FirstOrDefault();
        string currentStage = currentSection?.SectionId ?? section;
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
        TempData[TempDataKeys.PreviousStage] = previousStage;
        TempData[TempDataKeys.PreviousCategory] = previousCategory;
        TempData[TempDataKeys.CurrentStage] = currentStage;

        // Return the navigation DTO with all relevant navigation information
        return new NavigationDto
        {
            PreviousCategory = previousCategory,
            PreviousStage = previousStage,
            CurrentCategory = currentCategory,
            CurrentStage = currentStage,
            NextCategory = nextCategory,
            NextStage = nextStage
        };
    }
}