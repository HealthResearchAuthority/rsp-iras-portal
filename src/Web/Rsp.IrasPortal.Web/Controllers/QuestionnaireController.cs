using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.ProjectRecord.Controllers;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = Workspaces.MyResearch)]
public class QuestionnaireController
(
    IApplicationsService applicationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService questionSetService,
    IRtsService rtsService,
    IValidator<QuestionnaireViewModel> validator
) : ProjectRecordControllerBase(respondentService)
{
    private readonly IRespondentService _respondentService = respondentService;

    // Index view name
    private const string Index = nameof(Index);

    /// <summary>
    /// Resumes the application for the categoryId
    /// </summary>
    /// <param name="projectRecordId">Application Id</param>
    /// <param name="categoryId">CategoryId to resume from</param>
    /// <param name="validate">Indicates whether to validate or not</param>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    public async Task<IActionResult> Resume(string projectRecordId, string categoryId, string validate = "False", string? sectionId = null)
    {
        // load existing application in session
        if (await LoadApplication(projectRecordId) == null)
        {
            return NotFound();
        }

        // get the responent answers for the category
        var respondentServiceResponse = await _respondentService.GetRespondentAnswers(projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        IOrderedEnumerable<QuestionSectionsResponse>? questionSections = null;

        if (sectionId == null)
        {
            // get the questions for the category
            var questionSectionsResponse = await questionSetService.GetQuestionSections();

            if (!questionSectionsResponse.IsSuccessStatusCode)
            {
                // return the generic error page
                return this.ServiceError(questionSectionsResponse);
            }

            questionSections = questionSectionsResponse
                .Content?
                .Where(section => section.QuestionCategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(section => section.Sequence);

            // Ensure questionSections is not null and has elements
            if (questionSections?.Any() == true && validate == bool.FalseString)
            {
                // Get the first question section for the given categoryId
                var firstSection = questionSections.First();
                sectionId = firstSection.SectionId;
            }
        }

        var sectionIdOrDefault = sectionId ?? string.Empty;
        var questionsSetServiceResponse = await questionSetService.GetQuestionSet(sectionIdOrDefault);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(questionsSetServiceResponse);
        }

        // get the respondent answers and questions
        var respondentAnswers = respondentServiceResponse.Content!;
        var questions = questionsSetServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questions, true);

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);
        }

        if (validate == bool.FalseString)
        {
            // save the list of QuestionViewModel for the section in session to get it later
            HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));
        }
        else
        {
            // save the list of QuestionViewModel for all sections in session to get later
            foreach (var section in questionSections!)
            {
                var sectionQuestions = questionnaire.Questions.Where(q => q.SectionId == section.SectionId).ToList();
                HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{section.SectionId}", JsonSerializer.Serialize(sectionQuestions));
            }

            // validate the questionnaire. The application is being resumed from the SubmitApplication page
            await this.ValidateQuestionnaire(validator, questionnaire);

            TempData.TryAdd(TempDataKeys.BackRoute, "pov:index");

            // return the review page with errors if they exist
            return RedirectToAction("SubmitApplication", new { projectRecordId });
        }

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
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    public async Task<IActionResult> DisplayQuestionnaire(string sectionId, bool reviewAnswers = false)
    {
        // get the questions for the category
        var questionSectionsResponse = await questionSetService.GetQuestionSections();

        // return the view if successfull
        if (questionSectionsResponse.IsSuccessStatusCode)
        {
            var questions = default(List<QuestionViewModel>);

            // get the existing questionnaire for the category if exists in the session
            if (HttpContext.Session.Keys.Contains($"{SessionKeys.Questionnaire}:{sectionId}"))
            {
                var questionsJson = HttpContext.Session.GetString($"{SessionKeys.Questionnaire}:{sectionId}")!;

                questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(questionsJson)!;
            }

            // questionnaire doesn't exist in session so
            // get questions from the database for the category
            if (questions == null || questions.Count == 0)
            {
                var response = await questionSetService.GetQuestionSet(sectionId: sectionId);

                // return the view if successfull
                if (response.IsSuccessStatusCode)
                {
                    // set the active stage for the category
                    await SetStage(sectionId);

                    // convert the questions response to QuestionnaireViewModel
                    var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(response.Content!);

                    // store the questions to load again if there are validation errors on the page
                    HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));

                    // store organisation name to display in org autocomplete field
                    ViewBag.DisplayName = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsService, questionnaire.Questions, getIdFromQuestionsFirstAnswer: true);

                    return View(Index, questionnaire);
                }

                // return error page as api wasn't successful
                return this.ServiceError(response);
            }

            // set the active stage for the category
            await SetStage(sectionId);

            // store organisation name to display in org autocomplete field
            ViewBag.DisplayName = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsService, questions);

            // if we have questions in the session
            // then return the view with the model
            return View(Index, new QuestionnaireViewModel
            {
                CurrentStage = sectionId,
                Questions = questions,
                ReviewAnswers = reviewAnswers
            });
        }

        // return error page as api wasn't successful
        return this.ServiceError(questionSectionsResponse);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, string searchedPerformed, bool autoSearchEnabled, bool submit = false, string saveAndContinue = "False", string saveForLater = "False")
    {
        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(HttpContext.Session.GetString($"{SessionKeys.Questionnaire}:{model.CurrentStage}")!)!;

        // update the model with the answeres
        // provided by the applicant
        model.UpdateWithAnswers(model.Questions, questions);

        // override the submitted model
        // with the updated model with answers and rules
        model.Questions = questions;

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

                    if (!string.IsNullOrWhiteSpace(selectedOrg))
                    {
                        // pass name of organisation, to be displayed when there are validation errors on page
                        TempData.TryGetValue<OrganisationSearchResponse>(TempDataKeys.SponsorOrganisations, out var response, true);

                        if (response?.Organisations != null)
                        {
                            var selectedOrganisation = response.Organisations.FirstOrDefault(o => o.Id == selectedOrg);
                            if (selectedOrganisation != null)
                            {
                                model.SponsorOrgSearch.DisplayName = selectedOrganisation.Name;
                            }
                        }
                    }
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

        // validate the questionnaire and save the result in tempdata
        // this is so we display the validation passed message or not
        var isValid = await this.ValidateQuestionnaire(validator, model);

        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        // get the application from the session
        // to get the projectApplicationId
        var application = this.GetApplicationFromSession();

        if (!isValid)
        {
            // set the previous, current and next stages
            await SetStage(model.CurrentStage!);
            model.ReviewAnswers = submit;
            return View(Index, model);
        }

        // ------------------Save Project Record Answers Answers-------------------------
        await SaveProjectRecordAnswers(application.Id, questions);

        // set the previous, current and next stages
        var navigation = await SetStage(model.CurrentStage);

        // save the questions in the session
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{navigation.CurrentStage}", JsonSerializer.Serialize(questions));

        // user clicks on Proceed to submit button
        if (submit)
        {
            return RedirectToAction(nameof(SubmitApplication), new { projectRecordId = application.Id });
        }

        // user clicks on the SaveAndContinue button
        // so we need to resume from the next stage
        if (saveAndContinue == bool.TrueString)
        {
            // if the user is at the last stage and clicks on Save and Continue
            if (string.IsNullOrWhiteSpace(navigation.NextStage))
            {
                return RedirectToAction(nameof(SubmitApplication), new { projectRecordId = application.Id });
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
            return RedirectToAction("Index", "ProjectOverview", new { projectRecordId = application.Id });
        }

        // continue rendering the questionnaire if the above conditions are not true
        return RedirectToAction(nameof(DisplayQuestionnaire), new
        {
            navigation.NextCategory,
            navigation.NextStage
        });
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    /// <param name="projectRecordId">ApplicationId to submit</param>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    public async Task<IActionResult> SubmitApplication(string projectRecordId)
    {
        var categoryId = (TempData.Peek(TempDataKeys.CategoryId) as string)!;

        // get the responent answers for the category
        var respondentServiceResponse = await _respondentService.GetRespondentAnswers(projectRecordId, categoryId);

        // get the questions for all categories
        var questionSetServiceResponse = await questionSetService.GetQuestionSet();

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
        var questions = questionSetServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questions);

        // if we have answers, update the model with the provided answers
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        // use name for organisation from RTS
        ViewBag.DisplayName = await SponsorOrganisationNameHelper.GetSponsorOrganisationNameFromQuestions(rtsService, questionnaire.Questions);

        return View("ReviewAnswers", questionnaire);
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Update)]
    public async Task<IActionResult> ConfirmProjectDetails()
    {
        var categoryId = (TempData.Peek(TempDataKeys.CategoryId) as string)!;

        // get the application from the session
        // to get the projectApplicationId
        var application = this.GetApplicationFromSession();

        // get the respondent answers for the category
        var respondentServiceResponse = await _respondentService.GetRespondentAnswers(application.Id, categoryId);

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        // get the questions for all categories
        var questionSetServiceResponse = await questionSetService.GetQuestionSet();

        // return the error view if unsuccessfull
        if (!questionSetServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(questionSetServiceResponse);
        }

        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content);

        var respondentAnswers = respondentServiceResponse.Content!;

        // if we have answers, update the model with the provided answers
        questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

        var isValid = await this.ValidateQuestionnaire(validator, questionnaire, true);

        if (!isValid)
        {
            return View("ReviewAnswers", questionnaire);
        }

        // update the application status to Submitted
        var updateApplicationResponse = await applicationsService.UpdateApplication
        (
            new IrasApplicationRequest
            {
                Id = application.Id,
                ShortProjectTitle = application.ShortProjectTitle,
                FullProjectTitle = application.FullProjectTitle,
                StartDate = application.CreatedDate,
                Status = ProjectRecordStatus.Active,
                CreatedBy = application.CreatedBy,
                UpdatedBy = application.UpdatedBy,
                UserId = this.GetUserIdFromContext(),
                IrasId = application.IrasId
            }
        );

        if (!updateApplicationResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateApplicationResponse);
        }

        return RedirectToAction("ProjectRecordCreated");
    }

    /// <summary>
    /// Displays the 'Project record created' page with IRAS ID and project title
    /// </summary>
    [Authorize(Policy = Permissions.MyResearch.ProjectRecord_Read)]
    public async Task<IActionResult> ProjectRecordCreated()
    {
        var categoryId = (TempData.Peek(TempDataKeys.CategoryId) as string)!;

        // get the application from the session
        // to get the projectApplicationId
        var application = this.GetApplicationFromSession();

        // get the application details from the database
        var applicationResponse = await applicationsService.GetProjectRecord(application.Id);

        if (!applicationResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(applicationResponse);
        }

        // get the respondent answers for the category
        var respondentServiceResponse = await _respondentService.GetRespondentAnswers(application.Id, categoryId);

        // return the error view if unsuccessfull
        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the error page
            return this.ServiceError(respondentServiceResponse);
        }

        var projectRecord = applicationResponse.Content!;
        var answers = respondentServiceResponse.Content!;

        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;

        return View((projectRecord.IrasId, titleAnswer, projectRecord.Id));
    }

    /// <summary>
    /// Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination. Defults to 5 if not provided.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(QuestionnaireViewModel model, string? role, int? pageSize = 5, int pageIndex = 1)
    {
        var returnUrl = TempData.Peek(TempDataKeys.OrgSearchReturnUrl) as string;

        // override the submitted model
        // with the updated model with answers
        model.Questions = GetQuestionsFromSession(model);

        // get the application from the session
        // to get the applicationId
        var application = this.GetApplicationFromSession();

        // set the previous, current and next stages
        await SetStage(model.CurrentStage!);

        TempData.TryAdd(TempDataKeys.SponsorOrgSearched, "searched:true");

        // when search is performed, empty the currently selected organisation
        model.SponsorOrgSearch.SelectedOrganisation = string.Empty;

        // save the list of QuestionViewModel in session to get it later
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.Serialize(model.Questions));

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

    private List<QuestionViewModel> GetQuestionsFromSession(QuestionnaireViewModel model)
    {
        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(HttpContext.Session.GetString($"{SessionKeys.Questionnaire}:{model.CurrentStage}")!)!;

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
    /// Loads the existing application from the database
    /// </summary>
    /// <param name="projectApplicationId">Application Id</param>
    private async Task<IrasApplicationResponse?> LoadApplication(string projectApplicationId)
    {
        // get the application by id
        var response = await applicationsService.GetProjectRecord(projectApplicationId);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var irasApplication = response.Content!;

        // save the application in session
        HttpContext.Session.SetString(SessionKeys.ProjectRecord, JsonSerializer.Serialize(irasApplication));

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
        // Get the current categoryId from TempData
        var categoryId = (TempData.Peek(TempDataKeys.CategoryId) as string)!;

        // Fetch previous, current, and next section responses from the question set service
        var previousResponse = await questionSetService.GetPreviousQuestionSection(section);
        var currentResponse = await questionSetService.GetQuestionSections();
        var nextResponse = await questionSetService.GetNextQuestionSection(section);

        // Extract previous stage and category if available and matches the current category
        string previousStage = (previousResponse.IsSuccessStatusCode, previousResponse.Content?.SectionId) switch
        {
            // the first section is only to display the short and full project titles in a readonly mode
            // the first section is driven by ProjectRecordController so we don't want to show previous category/stage for it
            (true, null) => string.Empty,
            (true, not null) when previousResponse.Content.Sequence != 1 => previousResponse.Content.QuestionCategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase) ? previousResponse.Content.SectionId : string.Empty,
            _ => string.Empty
        };

        string previousCategory = (previousResponse.IsSuccessStatusCode, previousResponse.Content?.QuestionCategoryId) switch
        {
            // the first section is only to display the short and full project titles in a readonly mode
            // the first section is driven by ProjectRecordController so we don't want to show previous category/stage for it
            (true, null) => string.Empty,
            (true, not null) when previousResponse.Content.Sequence != 1 => previousResponse.Content.QuestionCategoryId.Equals(categoryId, StringComparison.OrdinalIgnoreCase) ? previousResponse.Content.QuestionCategoryId : string.Empty,
            _ => string.Empty
        };

        // Find the current section in the list of all sections for the current category
        var currentSection = currentResponse?.Content?.FirstOrDefault(s => s.SectionId == section && s.QuestionCategoryId == categoryId);
        string currentStage = currentSection?.SectionId ?? section;
        string currentCategory = currentSection?.QuestionCategoryId ?? "";

        // Extract next stage and category if available and matches the current category
        string nextStage = (nextResponse.IsSuccessStatusCode, nextResponse.Content?.SectionId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => nextResponse.Content.QuestionCategoryId == categoryId ? nextResponse.Content.SectionId : string.Empty,
            _ => string.Empty
        };

        string nextCategory = (nextResponse.IsSuccessStatusCode, nextResponse.Content?.QuestionCategoryId) switch
        {
            (true, null) => string.Empty,
            (true, not null) => nextResponse.Content.QuestionCategoryId == categoryId ? nextResponse.Content.QuestionCategoryId : string.Empty,
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