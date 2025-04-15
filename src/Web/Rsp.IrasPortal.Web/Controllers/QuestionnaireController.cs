using System.Text.Json;
using AspNetCoreGeneratedDocument;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = "IsUser")]
public class QuestionnaireController(IApplicationsService applicationsService, IRespondentService respondentService, IQuestionSetService questionSetService, IValidator<QuestionnaireViewModel> validator) : Controller
{
    // Index view name
    private const string Index = nameof(Index);

    /// <summary>
    /// Resumes the application for the categoryId
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    /// <param name="categoryId">CategoryId to resume from</param>
    /// <param name="validate">Indicates whether to validate or not</param>
    public async Task<IActionResult> Resume(string applicationId, string categoryId, string validate = "False", string? sectionId = null)
    {
        // load existing application in session
        if (await LoadApplication(applicationId) == null)
        {
            return NotFound();
        }

        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(applicationId, categoryId);

        if (!respondentServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            return this.ServiceError(respondentServiceResponse);
        }

        if(sectionId ==null)
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
                var firstSection = questionSections.FirstOrDefault(qs => qs.QuestionCategoryId == categoryId);

                if (firstSection != null)
                {
                    sectionId = firstSection.SectionId;
                }
            }
        }

        var sectionIdOrDefault = sectionId ?? string.Empty;
        var questionsSetServiceResponse = await questionSetService.GetQuestions(categoryId, sectionIdOrDefault);

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
        var questionnaire = BuildQuestionnaireViewModel(questions);

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            UpdateWithAnswers(respondentAnswers, questionnaire.Questions);
        }

        // save the list of QuestionViewModel in session to get it later
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));

        // add the applicationId in the TempData to be retrieved in the view
        TempData.TryAdd(TempDataKeys.ApplicationId, applicationId);

        // this is where the questionnaire will resume
        var navigationDto = SetStage(sectionId);

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
            return View(Index, questionnaire);
        }

        // continue to resume for the category Id & 
        return RedirectToAction(nameof(DisplayQuestionnaire), new
        {
            categoryId,
            sectionId

        });
    }

    /// <summary>
    /// Renders all of the questions for the categoryId
    /// </summary>
    /// <param name="categoryId">CategoryId of the questions to be rendered</param>
    ///<param name="sectionId">sectionId of the questions to be rendered</param>
    public async Task<IActionResult> DisplayQuestionnaire(string? categoryId, string? sectionId)
    {
        if (categoryId == null && sectionId == null)
        {
            return RedirectToAction("MyApplications", "Application");
        }

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
                // If section id is null get the first section id

                if (sectionId == null)
                {
                    var questionSections = questionSectionsResponse.Content;
                    // Ensure questionSections is not null and has elements
                    if (questionSections != null && questionSections.Any())
                    {
                        // Get the first question section for the given categoryId
                        var firstSection = questionSections.FirstOrDefault(qs => qs.QuestionCategoryId == categoryId);

                        if (firstSection != null)
                        {
                            sectionId = firstSection.SectionId;
                        }
                    }
                }

                // get the questions for the category
                var categoryIdOrDefault = categoryId ?? string.Empty; // Default to an empty string if categoryId is null
                var sectionIdOrDefault = sectionId ?? string.Empty;   // Default to an empty string if sectionId is null

                var response = await questionSetService.GetQuestions(categoryIdOrDefault, sectionIdOrDefault);


                // return the view if successfull
                if (response.IsSuccessStatusCode)
                {
                    // set the active stage for the category
                    SetStage(sectionId);

                    var questionnaire = BuildQuestionnaireViewModel(response.Content!);

                    // store the questions to load again if there are validation errors on the page
                    HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{sectionId}", JsonSerializer.Serialize(questionnaire.Questions));

                    return View(Index, questionnaire);
                }

                // return error page as api wasn't successful
                return this.ServiceError(response);
            }

            if (string.IsNullOrEmpty(sectionId))
            {
                return RedirectToAction("MyApplications", "Application"); // Safe fallback
            }

            // set the active stage for the category
            SetStage(sectionId);

            // if we have questions in the session
            // then return the view with the model
            return View(Index, new QuestionnaireViewModel
            {
                CurrentStage = sectionId,
                Questions = questions
            });
        }

        // return error page as api wasn't successful
        return this.ServiceError(questionSectionsResponse);
    }

    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> SaveResponses(QuestionnaireViewModel model, string categoryId = "", string submit = "False", string saveAndContinue = "False", string saveForLater = "False")
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
            // store the applicationId in the TempData to get in the view
            TempData.TryAdd(TempDataKeys.ApplicationId, application.ApplicationId);

            // set the previous, current and next stages
            SetStage(model.CurrentStage!);

            return View(Index, model);
        }

        // save the responses
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new RespondentAnswersRequest
        {
            ApplicationId = application.ApplicationId,
            RespondentId = respondentId
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

        // user clicks on Proceed to submit button
        if (submit == bool.TrueString)
        {
            return RedirectToAction(nameof(SubmitApplication), new { applicationId = application.ApplicationId });
        }

        // add the applicationId in the tempdata
        TempData.TryAdd(TempDataKeys.ApplicationId, application.ApplicationId);

        // set the previous, current and next stages
        var navigation = SetStage(model.CurrentStage);

        // save the questions in the session
        HttpContext.Session.SetString($"{SessionKeys.Questionnaire}:{navigation.CurrentStage}", JsonSerializer.Serialize(questions));

        // user clicks on the SaveAndContinue button
        // so we need to resume from the next stage
        if (saveAndContinue == bool.TrueString)
        {
            // if the user is at the last stage and clicks on Save and Continue
            if (string.IsNullOrWhiteSpace(navigation.NextStage))
            {
                return RedirectToAction(nameof(SubmitApplication), new { applicationId = application.ApplicationId });
            }

            // otherwise resume from the NextStage in sequence
            return RedirectToAction(nameof(Resume), new
            {
                applicationId = application.ApplicationId, categoryId = navigation.NextCategory, sectionId = navigation.NextStage

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
                applicationId = application.ApplicationId,
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

    /// <summary>
    /// Performs the validation when user clicks on validate button
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    /// <remarks>
    /// Some of categories have large number of questions and it will prvent submission of
    /// so many form values. Hence, we need to increase the ValueCountLimit
    /// </remarks>
    [RequestFormLimits(ValueCountLimit = int.MaxValue)]
    [HttpPost]
    public async Task<IActionResult> Validate(QuestionnaireViewModel model)
    {
        // get the questionnaire from the session
        // and deserialize it
        var questions = JsonSerializer.Deserialize<List<QuestionViewModel>>(HttpContext.Session.GetString($"{SessionKeys.Questionnaire}:{model.CurrentStage}")!)!;

        // update the model with the answeres
        // provided by the applicant
        foreach (var question in questions)
        {
            var response = model.Questions.Find(q => q.Index == question.Index);

            question.SelectedOption = response?.SelectedOption;
            question.Answers = response?.Answers ?? [];
            question.AnswerText = response?.AnswerText;
        }

        // override the submitted model
        // with the updated model with answers
        model.Questions = questions;

        // validate the questionnaire and save the result in tempdata
        // this is so we display the validation passed message or not
        ViewData[ViewDataKeys.IsQuestionnaireValid] = await ValidateQuestionnaire(model);

        // get the application from the session
        // to get the applicationId
        var application = this.GetApplicationFromSession();

        // store the applicationId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.ApplicationId, application.ApplicationId);

        // set the previous, current and next stages
        SetStage(model.CurrentStage!);

        return View(Index, model);
    }

    /// <summary>
    /// Gets all questions for the application. Validates for each category
    /// and display the progress of the application
    /// </summary>
    /// <param name="applicationId">ApplicationId to submit</param>
    [FeatureGate("Action.ProceedToSubmit")]
    public async Task<IActionResult> SubmitApplication(string applicationId)
    {
        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(applicationId);

        // get the questions for all categories
        var questionSetServiceResponse = await questionSetService.GetQuestions();

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

        // define the questionnaire validation state dictionary
        var questionnaireValidationState = new Dictionary<string, string>();

        var respondentAnswers = respondentServiceResponse.Content!;
        var questions = questionSetServiceResponse.Content!;

        // validate each category
        foreach (var questionsResponse in questions.ToLookup(q => q.Category))
        {
            // build the QuestionnaireViewModel for each category
            var questionnaire = BuildQuestionnaireViewModel(questionsResponse);

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

                // if the validation passess add the completed state otherwise incomplete state
                questionnaireValidationState.Add(category, result.IsValid ? "Completed" : "Incomplete");
            }
            else
            {
                // no answers are provided yet for the category
                // so add not entered to the validationstate
                questionnaireValidationState.Add(category, "Not Entered");
            }
        }

        // set applicationIsInvalid if the validatestate dictionary contains
        // incomplete or not entered. This will allow us to disable the
        // submit button in the view
        if (questionnaireValidationState.ContainsValue("Incomplete") ||
            questionnaireValidationState.ContainsValue("Not Entered"))
        {
            ViewData[ViewDataKeys.IsApplicationValid] = false;
        }

        return View(questionnaireValidationState);
    }

    /// <summary>
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
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
    /// Builds the QuestionnaireViewModel from the QuestionsResponse
    /// </summary>
    /// <param name="response">QuestionsResponse model for all the questions or for the category</param>
    private static QuestionnaireViewModel BuildQuestionnaireViewModel(IEnumerable<QuestionsResponse> response)
    {
        // order the questions by SectionId and Sequence
        var questions = response
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

        var questionnaire = new QuestionnaireViewModel();

        // build the questionnaire view model
        // we need to order the questions by section and sequence
        // and also need to assign the index to the question so the multiple choice
        // answsers can be linked back to the question
        foreach (var (question, index) in questions)
        {
            questionnaire.Questions.Add(new QuestionViewModel
            {
                Index = index,
                QuestionId = question.QuestionId,
                Category = question.Category,
                SectionId = question.SectionId,
                Section = question.Section,
                Sequence = question.Sequence,
                Heading = question.Heading,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                DataType = question.DataType,
                IsMandatory = question.IsMandatory,
                IsOptional = question.IsOptional,
                Rules = question.Rules,
                Answers = question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                }).ToList()
            });
        }

        return questionnaire;
    }

    /// <summary>
    /// Loads the existing application from the database
    /// </summary>
    /// <param name="applicationId">Application Id</param>
    private async Task<IrasApplicationResponse?> LoadApplication(string applicationId)
    {
        // get the application by id
        var response = await applicationsService.GetApplication(applicationId);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var irasApplication = response.Content!;

        // save the application in session
        HttpContext.Session.SetString(SessionKeys.Application, JsonSerializer.Serialize(irasApplication));

        return irasApplication;
    }



    /// <summary>
    /// Sets the Previous, Current, and Next stages required for navigation.
    /// </summary>
    /// <param name="section">Section of the current stage</param>
    private NavigationDto SetStage(string section)
    {
        var previousResponse = questionSetService.GetPreviousQuestionSection(section).Result;
        var currentResponse = questionSetService.GetQuestionSections().Result;
        var nextResponse = questionSetService.GetNextQuestionSection(section).Result;

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