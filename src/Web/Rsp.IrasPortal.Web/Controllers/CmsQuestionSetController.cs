using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "cmsqnc:[action]")]
[Authorize(Policy = "IsUser")]
public class CmsQuestionSetController(ICmsQuestionsetService questionSetService,
    IApplicationsService applicationsService,
    IRespondentService respondentService,
     IRtsService rtsService,
    IValidator<QuestionnaireViewModel> validator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? categoryId, string? sectionId, bool reviewAnswers = false, string? questionSetId = null)
    {
        var questionSet = await questionSetService.GetQuestionSet(sectionId, questionSetId);

        var questionsObject = questionSet.Content;

        var viewModelData = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsObject);
        if (viewModelData != null)
        {
            viewModelData.CurrentStage = viewModelData.Questions?.FirstOrDefault()?.SectionId;
            TempData[TempDataKeys.CurrentStage] = viewModelData.Questions?.FirstOrDefault()?.SectionId;
        }

        return View(viewModelData);
    }

    public async Task<IActionResult> Resume(string projectRecordId, string categoryId, string validate = "False", string? sectionId = null)
    {
        // load existing application in session
        if (await LoadApplication(projectRecordId) == null)
        {
            return NotFound();
        }

        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(projectRecordId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        if (sectionId == null)
        {
            // get the questions for the category
            var questionSectionsResponse = await questionSetService.GetQuestionSections();

            if (!questionSectionsResponse.IsSuccessStatusCode)
            {
                // return the generic error page
                return this.ServiceError(questionSectionsResponse);
            }

            var questionSections = questionSectionsResponse.Content;
            // Ensure questionSections is not null and has elements
            if (questionSections != null && questionSections.Any())
            {
                // Get the first question section for the given categoryId
                var firstSection = questionSections.FirstOrDefault();

                if (firstSection != null)
                {
                    sectionId = firstSection.SectionId;
                }
            }
        }

        var sectionIdOrDefault = sectionId ?? string.Empty;
        var questionsSetServiceResponse = await questionSetService.GetQuestionSet(sectionId);

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
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questions);

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            UpdateWithAnswers(respondentAnswers, questionnaire.Questions);
        }

        // save the list of QuestionViewModel in session to get it later
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));

        TempData.TryAdd(TempDataKeys.ProjectRecordId, projectRecordId);
        // this is where the questionnaire will resume
        var navigationDto = await SetStage(sectionIdOrDefault);

        questionnaire.CurrentStage = navigationDto.CurrentStage;

        // validate the questionnaire. The
        // application is being resumed from the
        // SubmitApplication page
        if (validate == bool.TrueString)
        {
            // this validation will addd model errors
            // to the ModelState dictionary
            await ValidateQuestionnaire(questionnaire);

            // return the view with errors
            return View("Index", questionnaire);
        }

        // continue to resume for the category Id &
        return RedirectToAction(nameof(DisplayQuestionnaire),
            new
            {
                sectionId,
                categoryId
            });
    }

    /// <summary>
    /// Renders all of the questions for the categoryId
    /// </summary>
    /// <param name="categoryId">CategoryId of the questions to be rendered</param>
    ///<param name="sectionId">sectionId of the questions to be rendered</param>
    public async Task<IActionResult> DisplayQuestionnaire(string categoryId, string sectionId, bool reviewAnswers = false)
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
                    var questionsObject = response.Content;
                    var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsObject);

                    // store the questions to load again if there are validation errors on the page
                    HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));

                    return View("Index", questionnaire);
                }

                // return error page as api wasn't successful
                return this.ServiceError(response);
            }

            // set the active stage for the category
            await SetStage(sectionId);

            // if we have questions in the session
            // then return the view with the model
            return View("Index", new QuestionnaireViewModel
                {
                CurrentStage = sectionId,
                Questions = questions,
                ReviewAnswers = reviewAnswers
            });
                }

        // return error page as api wasn't successful
        return this.ServiceError(questionSectionsResponse);
            }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, string categoryId = "", bool submit = false, string saveAndContinue = "False", string saveForLater = "False")
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

        // override the submitted model
        // with the updated model with answers
        model.Questions = questions;

        // validate the questionnaire and save the result in tempdata
        // this is so we display the validation passed message or not
        var isValid = await ValidateQuestionnaire(model);
        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        // get the application from the session
        // to get the applicationId
        var application = this.GetApplicationFromSession();

        if (!isValid)
        {
            TempData.TryAdd(TempDataKeys.ProjectRecordId, application.Id);
            // set the previous, current and next stages
            await SetStage(model.CurrentStage!);
            model.ReviewAnswers = submit;
            return View("Index", model);
    }

        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new RespondentAnswersRequest
    {
            ProjectRecordId = application.Id,
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
                "Boolean" or "Radio button" or "Look-up list" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            // build RespondentAnswers model
            request.RespondentAnswers.Add(new RespondentAnswerDto
            {
                QuestionId = question.QuestionId,
                AnswerText = question.AnswerText,
                CategoryId = question.Category,
                SectionId = question.SectionId,
                SelectedOption = question.SelectedOption,
                OptionType = optionType,
                VersionId = question.VersionId,
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
            var savedResponces = await respondentService.SaveRespondentAnswers(request);
        }

        TempData.TryAdd(TempDataKeys.ProjectRecordId, application.Id);
        // set the previous, current and next stages
        var navigation = await SetStage(model.CurrentStage);

        // save the questions in the session
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{navigation.CurrentStage}", JsonSerializer.Serialize(questions));

        // user clicks on Proceed to submit button
        if (submit)
        {
            return RedirectToAction(nameof(SubmitApplication), new { applicationId = application.Id });
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
            return RedirectToAction("ProjectOverview", "Application");
        }
        // user jumps to the next stage by clicking on the link
        // so we need to resume the application from there
        if (!string.IsNullOrWhiteSpace(navigation.NextStage))
            {
            return RedirectToAction(nameof(Resume), new
                {
                projectRecordId = application.Id,
                categoryId = navigation.NextCategory,
                sectionId = navigation.NextStage
            });
        }

        // continue rendering the questionnaire if the above conditions are not true
        return RedirectToAction(nameof(DisplayQuestionnaire), new
                    {
            navigation.NextCategory,
            navigation.NextStage
                    });
                }

    public async Task<IActionResult> SubmitApplication(string projectRecordId)
    {
        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(projectRecordId);

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
            throw new Exception("Error occured");
            }

        // define the questionnaire validation state dictionary
        var questionnaireValidationState = new Dictionary<string, string>();

        var respondentAnswers = respondentServiceResponse.Content!;
        var questions = questionSetServiceResponse.Content!;

        //var questionnaire = new QuestionnaireViewModel
        //{
        //    CurrentStage = string.Empty,
        //    Questions = new List<QuestionViewModel>()
        //};
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questions);

        // validate each category
        foreach (var questionsResponse in questionnaire.Questions.GroupBy(x => x.Category))
            {
            // build the QuestionnaireViewModel for each category
            //questionnaire = BuildQuestionnaireViewModel(questionsResponse);

            if (questionnaire.Questions.Count == 0)
                {
                continue;
            }

            var category = questionsResponse.Key;

            // get the answers for the category
            var answers = respondentAnswers.Where(r => r.CategoryId == category).ToList();

            ValidationContext<QuestionnaireViewModel> context;

            if (answers.Count > 0)
                    {
                // if we have answers, update the model with the provided answers
                UpdateWithAnswers(respondentAnswers, questionnaire.Questions);

                // using the FluentValidation, create a new context for the model
                context = new ValidationContext<QuestionnaireViewModel>(questionnaire);

                // this is required to get the questions in the validator
                // before the validation cicks in
                context.RootContextData["questions"] = questionnaire.Questions;

                // call the ValidateAsync to execute the validation
                // this will trigger the fluentvalidation using the injected validator if configured
                var result = await validator.ValidateAsync(context);
            }
        }

        // get the application from the session
        // to get the applicationId
        var application = this.GetApplicationFromSession();

        // store the irasId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.IrasId, application.IrasId);

        // store the first categoryId and applicationId in the TempData to get in the view
        TempData[TempDataKeys.CategoryId] = (questionnaire.Questions.GroupBy(q => q.Category)
        .OrderBy(g => g.First().Sequence).FirstOrDefault()?.Key);
        TempData[TempDataKeys.ProjectRecordId] = application.Id;

        return View("ReviewAnswers", questionnaire);
    }

    public async Task<IActionResult> ConfirmProjectDetails()
    {
        // get the application from the session
        // to get the applicationId
        var application = this.GetApplicationFromSession();

        // get the respondent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(application.Id);

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
            //return this.ServiceError(questionSetServiceResponse);
            throw new Exception("Error occured");
        }
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content);
        // define the questionnaire validation state dictionary
        var questionnaireValidationState = new Dictionary<string, string>();

        var respondentAnswers = respondentServiceResponse.Content!;
        //var questions = questionSetServiceResponse.Content!;

        // validate each category
        foreach (var questionsResponse in questionnaire.Questions.GroupBy(x => x.Category))
        {
            // build the QuestionnaireViewModel for each category
            //var questionnaire = BuildQuestionnaireViewModel(questionsResponse);

            if (questionnaire.Questions.Count == 0)
            {
                continue;
            }

            var category = questionsResponse.Key;

            // get the answers for the category
            var answers = respondentAnswers.Where(r => r.CategoryId == category).ToList();

            ValidationContext<QuestionnaireViewModel> context;

            // if we have answers, update the model with the provided answers
            UpdateWithAnswers(respondentAnswers, questionnaire.Questions);

            // using the FluentValidation, create a new context for the model
            context = new ValidationContext<QuestionnaireViewModel>(questionnaire);

            // this is required to get the questions in the validator
            // before the validation cicks in
            context.RootContextData["questions"] = questionnaire.Questions;
            context.RootContextData["ValidateMandatoryOnly"] = true;

            // store the irasId in the TempData to get in the view
            TempData.TryAdd(TempDataKeys.IrasId, application.IrasId);

            // store the first categoryId and applicationId in the TempData to get in the view
            TempData[TempDataKeys.CategoryId] = (questionnaire.Questions.GroupBy(q => q.Category)
            .OrderBy(g => g.First().Sequence).FirstOrDefault()?.Key);
            TempData[TempDataKeys.ProjectRecordId] = application.Id;

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

                return View("ReviewAnswers", questionnaire);
            }
        }

        return RedirectToAction("ProjectOverview", "Application");
    }

    /// <summary>
    /// Retrieves a list of organisations based on the provided name, role, and optional page size.
    /// </summary>
    /// <param name="role">The role of the organisation. Defaults to SponsorRole if not provided.</param>
    /// <param name="pageSize">Optional page size for pagination.</param>
    /// <returns>A list of organisation names or an error response.</returns>
    public async Task<IActionResult> SearchOrganisations(QuestionnaireViewModel model, string? role, int? pageSize)
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
        var searchResponse = pageSize is null ?
            await rtsService.GetOrganisationsByName(model.SponsorOrgSearch.SearchText!, role) :
            await rtsService.GetOrganisationsByName(model.SponsorOrgSearch.SearchText, role, pageSize.Value);

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

    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model)
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

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

    private async Task<NavigationDto> SetStage(string section)
    {
        var previousResponse = await questionSetService.GetPreviousQuestionSection(section);
        var currentResponse = await questionSetService.GetQuestionSections();
        var nextResponse = await questionSetService.GetNextQuestionSection(section);

        // Extracting previous stage and category
        string previousStage = previousResponse.IsSuccessStatusCode ? previousResponse.Content?.SectionId ?? "" : "";
        string previousCategory = previousResponse.IsSuccessStatusCode ? previousResponse.Content?.QuestionCategoryId ?? "" : "";

        // Extracting current stage and category
        var currentSection = currentResponse?.Content?.FirstOrDefault(s => s.SectionId == section);
        string currentStage = currentSection?.SectionId ?? section;
        string currentCategory = currentSection?.QuestionCategoryId ?? "";

        // Extracting next stage and category
        string nextStage = nextResponse.IsSuccessStatusCode ? nextResponse.Content?.SectionId ?? "" : "";
        string nextCategory = nextResponse.IsSuccessStatusCode ? nextResponse.Content?.QuestionCategoryId ?? "" : "";

        // Store in TempData
        TempData[TempDataKeys.PreviousStage] = previousStage;
        TempData[TempDataKeys.PreviousCategory] = previousCategory;
        TempData[TempDataKeys.CurrentStage] = currentStage;

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

    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model)
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

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

    private async Task<NavigationDto> SetStage(string section)
    {
        var previousResponse = await questionSetService.GetPreviousQuestionSection(section);
        var currentResponse = await questionSetService.GetQuestionSections();
        var nextResponse = await questionSetService.GetNextQuestionSection(section);

        // Extracting previous stage and category
        string previousStage = previousResponse.IsSuccessStatusCode ? previousResponse.Content?.SectionId ?? "" : "";
        string previousCategory = previousResponse.IsSuccessStatusCode ? previousResponse.Content?.QuestionCategoryId ?? "" : "";

        // Extracting current stage and category
        var currentSection = currentResponse?.Content?.FirstOrDefault(s => s.SectionId == section);
        string currentStage = currentSection?.SectionId ?? section;
        string currentCategory = currentSection?.QuestionCategoryId ?? "";

        // Extracting next stage and category
        string nextStage = nextResponse.IsSuccessStatusCode ? nextResponse.Content?.SectionId ?? "" : "";
        string nextCategory = nextResponse.IsSuccessStatusCode ? nextResponse.Content?.QuestionCategoryId ?? "" : "";

        // Store in TempData
        TempData[TempDataKeys.PreviousStage] = previousStage;
        TempData[TempDataKeys.PreviousCategory] = previousCategory;
        TempData[TempDataKeys.CurrentStage] = currentStage;

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