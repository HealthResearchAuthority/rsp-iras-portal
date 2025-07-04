using System.Text.Json;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "cmsqnc:[action]")]
[Authorize(Policy = "IsUser")]
public class CmsQuestionSetController(ICmsQuestionSetServiceClient questionSetService,
    IApplicationsService applicationsService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? categoryId, string? sectionId, bool reviewAnswers = false, string questionSetId = null)
    {
        var model = new List<QuestionsResponse>();
        var questionSet = await questionSetService.GetQuestionSet(sectionId, questionSetId);

        var questionsObject = questionSet.Content;

        var viewModelData = BuildQuestionnaireViewModel(questionsObject);
        viewModelData.CurrentStage = viewModelData.Questions.FirstOrDefault().SectionId;
        TempData[TempDataKeys.CurrentStage] = viewModelData.Questions.FirstOrDefault().SectionId;

        return View(viewModelData);
    }

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

        if (sectionId == null)
        {
            // get the questions for the category
            var questionSectionsResponse = await questionSetService.GetQuestionSections();

            if (!questionSectionsResponse.IsSuccessStatusCode)
            {
                // return the generic error page
                throw new Exception("Error occured");
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
        var questionsSetServiceResponse = await questionSetService.GetQuestionSet(sectionId);

        // return error page if unsuccessfull
        if (!questionsSetServiceResponse.IsSuccessStatusCode)
        {
            // return the generic error page
            throw new Exception("Error occured");
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
        TempData.TryAdd(TempDataKeys.ProjectRecordId, applicationId);

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
        return RedirectToAction(nameof(Index),
            new
            {
                sectionId,
                categoryId
            });
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
            // store the applicationId in the TempData to get in the view
            TempData.TryAdd(TempDataKeys.ProjectRecordId, application.Id);

            // store the irasId in the TempData to get in the view
            TempData.TryAdd(TempDataKeys.IrasId, application.IrasId);

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

        // add the applicationId in the tempdata
        TempData.TryAdd(TempDataKeys.ProjectRecordId, application.Id);

        // store the irasId in the TempData to get in the view
        TempData.TryAdd(TempDataKeys.IrasId, application.IrasId);

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
                return RedirectToAction(nameof(SubmitApplication), new { applicationId = application.Id });
            }

            // otherwise resume from the NextStage in sequence
            return RedirectToAction(nameof(Resume), new
            {
                applicationId = application.Id,
                categoryId = navigation.NextCategory,
                sectionId = navigation.NextStage
            });
        }

        if (saveForLater == bool.TrueString)
        {
            //TempData[TempDataKeys.ShortProjectTitle] = model.GetShortProjectTitle();
            //TempData[TempDataKeys.CategoryId] = model.GetFirstCategory();
            //TempData[TempDataKeys.ProjectRecordId] = application.Id;

            return RedirectToAction("ProjectOverview", "Application");
        }
        // user jumps to the next stage by clicking on the link
        // so we need to resume the application from there
        if (!string.IsNullOrWhiteSpace(navigation.NextStage))
        {
            return RedirectToAction(nameof(Resume), new
            {
                applicationId = application.Id,
                categoryId = navigation.NextCategory,
                sectionId = navigation.NextStage
            });
        }

        // continue rendering the questionnaire if the above conditions are not true
        return RedirectToAction(nameof(Index), new
        {
            navigation.NextCategory,
            navigation.NextStage
        });
    }

    public async Task<IActionResult> SubmitApplication(string applicationId)
    {
        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetRespondentAnswers(applicationId);

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
        var questionnaire = BuildQuestionnaireViewModel(questions);

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
        var questionnaire = BuildQuestionnaireViewModel(questionSetServiceResponse.Content);
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

    private async Task<IrasApplicationResponse?> LoadApplication(string applicationId)
    {
        // get the application by id
        var response = await applicationsService.GetProjectRecord(applicationId);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var irasApplication = response.Content!;

        // save the application in session
        HttpContext.Session.SetString(SessionKeys.ProjectRecord, JsonSerializer.Serialize(irasApplication));

        return irasApplication;
    }

    private static IEnumerable<QuestionsResponse> QuestionTransformer(SectionModel section)
    {
        var response = new List<QuestionsResponse>();
        foreach (var (question, i) in section.Questions.Select((value, i) => (value, i)))
        {
            var questionTransformed = new QuestionsResponse
            {
                IsMandatory = (question.Conformance == "Mandatory"),
                Heading = (i + 1).ToString(),
                Sequence = i + 1,
                Section = section.SectionName,
                SectionId = section.Id,
                QuestionId = question.Id,
                QuestionText = question.Name,
                DataType = question.AnswerDataType,
                QuestionType = question.QuestionFormat,
                Category = section.SectionName,
                Answers = new List<AnswerDto>(),
                Rules = new List<RuleDto>(),
                GuidanceComponents = question.GuidanceComponents,
            };

            if (question.Answers != null && question.Answers.Any())
            {
                foreach (var answer in question.Answers)
                {
                    questionTransformed.Answers.Add(new AnswerDto
                    {
                        AnswerId = answer.Id,
                        AnswerText = answer.OptionName
                    });
                }
            }

            if (question.ValidationRules != null && question.ValidationRules.Any())
            {
                foreach (var (validationRule, y) in question.ValidationRules.Select((value, y) => (value, y)))
                {
                    var ruleConditions = new List<ConditionDto>();
                    var transformedRule = validationRule.Adapt<RuleDto>();
                    transformedRule.Sequence = y;
                    transformedRule.ParentQuestionId = validationRule.ParentQuestion?.Id?.ToString();

                    foreach (var condition in validationRule.Conditions)
                    {
                        var conditionModel = condition.Adapt<ConditionDto>();
                        conditionModel.IsApplicable = true;
                        if (condition.ParentOptions != null)
                        {
                            conditionModel.ParentOptions = condition.ParentOptions.Select(x => x.Id.ToString()).ToList();
                        }

                        ruleConditions.Add(conditionModel);
                    }

                    transformedRule.Conditions = ruleConditions;
                    questionTransformed.Rules.Add(transformedRule);
                }
            }

            response.Add(questionTransformed);
        }

        return response;
    }

    private static List<QuestionsResponse> ConvertToQuestionResponse(CmsQuestionSetResponse response)
    {
        var model = new List<QuestionsResponse>();
        foreach (var section in response.Sections)
        {
            if (section.Questions != null && section.Questions.Any())
            {
                var mappedQUestion = QuestionTransformer(section);
                model.AddRange(mappedQUestion);
            }
        }
        return model;
    }

    private static QuestionnaireViewModel BuildQuestionnaireViewModel(CmsQuestionSetResponse response)
    {
        var model = ConvertToQuestionResponse(response);

        // order the questions by SectionId and Sequence
        var questions = model
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

        var questionnaire = new QuestionnaireViewModel
        {
            GuidanceContent = response?.Sections?.FirstOrDefault()?.GuidanceComponents?.ToList() != null ?
            response.Sections.FirstOrDefault().GuidanceComponents.ToList() :
            []
        };

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
                ShortQuestionText = question.ShortQuestionText,
                QuestionType = question.QuestionType,
                DataType = question.DataType,
                IsOptional = question.IsOptional,
                Rules = question.Rules,
                IsMandatory = question.IsMandatory,
                GuidanceComponents = question.GuidanceComponents,
                Answers = question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                }).ToList()
            });
        }

        return questionnaire;
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