using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Extensions;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.AccessControl;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Helpers;
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
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<QuestionnaireViewModel> validator
) : Controller
{
    private const string SectionId = "pm-sponsor-reference";
    private const string DocumentDetailsSection = "pdm-document-metadata";
    protected readonly IProjectModificationsService projectModificationsService = projectModificationsService;
    protected readonly ICmsQuestionsetService cmsQuestionsetService = cmsQuestionsetService;

    protected async Task<(IActionResult?, ModificationDetailsViewModel?)> GetModificationDetails(Guid projectModificationId, string irasId, string shortTitle, string projectRecordId)
    {
        // Fetch the modification by its identifier
        var modificationResponse = await projectModificationsService.GetModification(projectRecordId, projectModificationId);

        // Short-circuit with a service error if the call failed
        if (!modificationResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(modificationResponse), null);
        }

        if (modificationResponse.Content is null)
        {
            return (this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Error = $"Error retrieving the modification for project record: {projectRecordId} modificationId: {projectModificationId}",
            }), null);
        }

        // Select the first (and only) modification result
        var modification = modificationResponse.Content;

        TempData[TempDataKeys.ProjectModification.ProjectModificationStatus] = modification.Status;

        // Build the base view model with project metadata
        return (null, new ModificationDetailsViewModel
        {
            ModificationId = modification.Id.ToString(),
            IrasId = irasId,
            ShortTitle = shortTitle,
            ModificationIdentifier = modification.ModificationIdentifier,
            Status = modification.Status,
            ProjectRecordId = projectRecordId,
            ModificationType = modification.ModificationType ?? Ranking.NotAvailable,
            Category = modification.Category ?? Ranking.NotAvailable,
            ReviewType = modification.ReviewType ?? Ranking.NotAvailable,
            DateCreated = DateHelper.ConvertDateToString(modification.CreatedDate)
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
        var modificationsResponse = await projectModificationsService.GetModificationChanges(modification.ProjectRecordId, Guid.Parse(modification.ModificationId!));

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

    protected async Task UpdateModificationWithChanges
    (
        StartingQuestionsDto initialQuestions,
        ModificationDetailsViewModel modification,
        IEnumerable<ProjectModificationChangeResponse> modificationChanges
    )
    {
        foreach (var change in modificationChanges.OrderByDescending(c => c.CreatedDate))
        {
            if (change.AreaOfChange == Guid.Empty.ToString() || change.SpecificAreaOfChange == Guid.Empty.ToString())
            {
                continue;
            }

            var areaOfChange = initialQuestions!.AreasOfChange.Find(area => area.AutoGeneratedId == change.AreaOfChange);

            var specificAreaOfChange = areaOfChange?.SpecificAreasOfChange.Find(area => area.AutoGeneratedId == change.SpecificAreaOfChange);

            // get the questions for the change
            var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(change.SpecificAreaOfChange);

            // get the responent answers for the change
            var respondentServiceResponse = await respondentService.GetModificationChangeAnswers(change.Id, modification.ProjectRecordId);

            var respondentAnswers = respondentServiceResponse.Content!;

            // convert the questions response to QuestionnaireViewModel
            var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);

            // Apply answers and trim questions using shared helper
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

            var questions = questionnaire.Questions;

            var changeModel = new ModificationChangeModel
            {
                ModificationChangeId = change.Id,
                ModificationType = change.ModificationType ?? Ranking.NotAvailable,
                Category = change.Category ?? Ranking.NotAvailable,
                ReviewType = change.ReviewType ?? Ranking.NotAvailable,
                AreaOfChangeName = areaOfChange?.OptionName ?? string.Empty,
                SpecificChangeName = specificAreaOfChange?.OptionName ?? string.Empty,
                SpecificAreaOfChangeId = specificAreaOfChange?.AutoGeneratedId ?? string.Empty,
                ChangeStatus = change.Status ?? string.Empty,
                Questions = questions ?? []
            };

            // show surfacing questions
            ModificationHelpers.ShowSurfacingQuestion(questions, changeModel, "ReviewAllChanges");

            // remove all the conditional questions without answers, these must have been
            // validated on the previous screen
            questions.RemoveAll(q => !(q.IsMandatory || q.IsOptional) && q.IsMissingAnswer());

            modification.ModificationChanges.Add(changeModel);
        }
    }

    protected async Task SaveModificationAnswers(Guid projectModificationId, string projectRecordId, List<QuestionViewModel> questions)
    {
        // save the responses
        var userId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;

        // to save the responses
        // we need to build the RespondentAnswerRequest
        // populate the RespondentAnswers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationId = projectModificationId,
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

    protected async Task<(IActionResult? Result, ModificationDetailsViewModel? Model)> PrepareModificationAsync
    (
        Guid projectModificationId,
        string irasId,
        string shortTitle,
        string projectRecordId
    )
    {
        var (modificationResult, model) = await GetModificationDetails(projectModificationId, irasId, shortTitle, projectRecordId);
        if (modificationResult is not null)
        {
            return (modificationResult, null);
        }

        var modification = model!;
        TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modification.ModificationIdentifier;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = modification.ModificationId;
        TempData[TempDataKeys.ProjectModification.OverallReviewType] = modification.ReviewType;
        TempData[TempDataKeys.IrasId] = irasId;

        var (changesResult, initialQuestions, modificationChanges) = await GetModificationChanges(modification);
        if (changesResult is not null)
        {
            return (changesResult, null);
        }

        await UpdateModificationWithChanges(initialQuestions!, modification, modificationChanges!);

        return (null, modification);
    }

    /// <summary>
    /// Builds a standard ProjectModificationDocumentRequest using TempData and HttpContext.
    /// </summary>
    protected ProjectModificationDocumentRequest BuildDocumentRequest()
    {
        var rawValue = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);

        Guid projectModificationId = rawValue switch
        {
            Guid g => g,                                                           // already a Guid
            string s when Guid.TryParse(s, out var parsed) => parsed,              // string Guid
            _ => throw new InvalidOperationException("ProjectModificationId not found or invalid")
        };

        return new ProjectModificationDocumentRequest
        {
            ProjectModificationId = projectModificationId,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            UserId = (HttpContext.Items[ContextItemKeys.UserId] as string)!,
        };
    }

    /// <summary>
    /// Fetches all uploaded modification documents and determines their detail completion status.
    /// </summary>
    protected async Task<List<DocumentSummaryItemDto>> GetDocumentCompletionStatuses(ProjectModificationDocumentRequest documentChangeRequest)
    {
        // Fetch CMS question set (document metadata requirements)
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(DocumentDetailsSection);
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Get uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationId,
            documentChangeRequest.ProjectRecordId);

        if (response?.StatusCode != HttpStatusCode.OK || response.Content == null)
            return [];

        // Evaluate each document's completeness
        var tasks = response.Content
            .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(a => GetDocumentSummary(a, questionnaire));

        return [.. await Task.WhenAll(tasks)];
    }

    protected async Task<QuestionnaireViewModel> PopulateAnswersFromDocuments(
    QuestionnaireViewModel questionnaire,
    IEnumerable<ProjectModificationDocumentAnswerDto> answers)
    {
        foreach (var question in questionnaire.Questions)
        {
            // Find the matching answer by QuestionId
            var match = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

            if (match != null)
            {
                question.AnswerText = match.AnswerText;
                question.SelectedOption = match.SelectedOption;

                // carry over OptionType (if you want to track Single/Multiple)
                question.QuestionType = match.OptionType ?? question.QuestionType;

                // map multiple answers into AnswerViewModel list
                if (match.Answers != null && match.Answers.Any())
                {
                    question.Answers = match.Answers
                        .ConvertAll(ans => new AnswerViewModel
                        {
                            AnswerId = ans,        // if ans is an ID
                            AnswerText = ans,      // or fetch the display text elsewhere if IDs map to text
                            IsSelected = true
                        })
;
                }
            }
        }

        return questionnaire;
    }

    /// <summary>
    /// Evaluates whether a single document�s answers are complete.
    /// </summary>
    private async Task<DocumentSummaryItemDto> GetDocumentSummary(ProjectModificationDocumentRequest a, QuestionnaireViewModel questionnaire)
    {
        var status = a.Status;

        if (!a.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
            a.Status.Equals(DocumentStatus.Uploaded, StringComparison.OrdinalIgnoreCase))
        {
            status = (await EvaluateDocumentCompletion(a.Id, questionnaire)
                ? DocumentDetailStatus.Incomplete
                : DocumentDetailStatus.Complete).ToString();
        }

        return new DocumentSummaryItemDto
        {
            DocumentId = a.Id,
            FileName = $"Add details for {a.FileName}",
            FileSize = a.FileSize ?? 0,
            BlobUri = a.DocumentStoragePath ?? string.Empty,
            Status = status
        };
    }

    /// <summary>
    /// Evaluates whether a single document�s answers are complete.
    /// </summary>
    protected async Task<bool> EvaluateDocumentCompletion(Guid documentId, QuestionnaireViewModel questionnaire)
    {
        // Fetch document answers
        var answersResponse = await respondentService.GetModificationDocumentAnswers(documentId);
        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
            ? answersResponse.Content ?? []
            : [];

        // Clone questionnaire to prevent shared mutation
        var clonedQuestionnaire = CloneQuestionnaire(questionnaire);

        // Populate with answers
        clonedQuestionnaire = await PopulateAnswersFromDocuments(clonedQuestionnaire, answers);

        // Validate questionnaire
        var isValid = await this.ValidateQuestionnaire(validator, clonedQuestionnaire, true);
        return !answers.Any() || !isValid;
    }

    protected async Task MapDocumentTypesAndStatusesAsync(
      QuestionnaireViewModel questionnaire,
      IEnumerable<ProjectOverviewDocumentDto> documents)
    {
        if (questionnaire?.Questions == null || documents == null)
            return;

        // 1. Locate the "Document Type" question
        var documentTypeQuestion = questionnaire.Questions
            .FirstOrDefault(q =>
                string.Equals(q.QuestionId?.ToString(),
                QuestionIds.SelectedDocumentType,
                StringComparison.OrdinalIgnoreCase));

        if (documentTypeQuestion?.Answers?.Any() != true)
            return;

        // **Answer dictionary for fast lookup**
        var answerLookup = documentTypeQuestion.Answers
            .ToDictionary(a => a.AnswerId, a => a.AnswerText, StringComparer.OrdinalIgnoreCase);

        // 2. Loop through documents
        foreach (var doc in documents)
        {
            // ---- A. Map DocumentType AnswerId → AnswerText ----
            if (!string.IsNullOrWhiteSpace(doc.DocumentType) &&
                answerLookup.TryGetValue(doc.DocumentType, out var friendlyName))
            {
                doc.DocumentType = friendlyName;
            }

            // ---- B. Update completion status ----
            if (!doc.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
                doc.Status.Equals(DocumentStatus.Uploaded, StringComparison.OrdinalIgnoreCase))
            {
                bool isIncomplete = await EvaluateDocumentCompletion(doc.Id, questionnaire);

                doc.Status = isIncomplete
                    ? DocumentDetailStatus.Incomplete.ToString()
                    : DocumentDetailStatus.Complete.ToString();
            }
        }
    }

    protected async Task<List<ModificationChangeModel>> UpdateModificationChanges(string projectRecordId, List<ModificationChangeModel> modificationChanges)
    {
        foreach (var modificationChange in modificationChanges)
        {
            // get the responent answers for the category
            var respondentServiceResponse = await respondentService.GetModificationChangeAnswers(modificationChange.ModificationChangeId, projectRecordId);

            // get the questions for the modification journey
            var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(modificationChange.SpecificAreaOfChangeId);

            // return the error view if unsuccessfull
            if (!respondentServiceResponse.IsSuccessStatusCode || !questionSetServiceResponse.IsSuccessStatusCode)
            {
                // return the modificationChanges unchanged in case of error
                return modificationChanges;
            }

            var respondentAnswers = respondentServiceResponse.Content!;

            // convert the questions response to QuestionnaireViewModel
            var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionSetServiceResponse.Content!);

            var questions = questionnaire.Questions;

            if (questions.Count == 0)
            {
                modificationChange.ChangeStatus = ModificationStatus.ChangeReadyForSubmission;
            }

            // Apply respondent answers to the questionnaire
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);

            // Validate the questionnaire (mandatory-only) using FluentValidation
            var result = await this.ValidateQuestionnaire(validator, questionnaire, true, false);

            modificationChange.ChangeStatus = result ?
                ModificationStatus.ChangeReadyForSubmission :
                ModificationStatus.Unfinished;

            // show surfacing questions
            ModificationHelpers.ShowSurfacingQuestion(questions, modificationChange, "ModificationDetails");
        }

        return modificationChanges;
    }

    protected async Task<QuestionnaireViewModel> BuildSponsorQuestionnaireViewModel(Guid projectModificationId, string projectRecordId, string categoryId)
    {
        // get the responent answers for the category
        var respondentServiceResponse = await respondentService.GetModificationAnswers(projectModificationId, projectRecordId, categoryId);

        var questionsSetServiceResponse = await cmsQuestionsetService.GetModificationQuestionSet(SectionId);

        // get the respondent answers and questions
        var respondentAnswers = respondentServiceResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(questionsSetServiceResponse.Content!, true);

        // if respondent has answerd any questions
        if (respondentAnswers.Any())
        {
            questionnaire.UpdateWithRespondentAnswers(respondentAnswers);
        }

        return new QuestionnaireViewModel
        {
            CurrentStage = SectionId,
            Questions = questionnaire.Questions
        };
    }

    protected async Task<(ServiceResponse<ProjectOverviewDocumentResponse>, QuestionnaireViewModel)> GetModificationDocuments(
        Guid projectModificationId,
        string documentDetailsSection,
        int pageNumber,
        int pageSize,
        string? sortField,
        string? sortDirection)
    {
        var searchQuery = new ProjectOverviewDocumentSearchRequest();

        // Fetch CMS question set
        var qsResponse = await cmsQuestionsetService.GetModificationQuestionSet(documentDetailsSection);

        // Build questionnaire
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(qsResponse.Content!);

        // Find the document type question
        var docTypeQuestion = questionnaire.Questions
            .FirstOrDefault(q => q.QuestionId == ModificationQuestionIds.DocumentType);

        // Populate DocumentTypes
        searchQuery.DocumentTypes = docTypeQuestion?.Answers?
            .ToDictionary(a => a.AnswerId, a => a.AnswerText) ?? new();

        // Allowed statuses based on user
        searchQuery.AllowedStatuses = User.GetAllowedStatuses(StatusEntitiy.Document);

        // Fetch documents
        var documents = await projectModificationsService.GetDocumentsForModification(
            projectModificationId,
            searchQuery,
            pageNumber,
            pageSize,
            sortField,
            sortDirection
        );

        return (documents, questionnaire);
    }

    /// <summary>
    /// Clones a questionnaire deeply to avoid shared references.
    /// </summary>
    private static QuestionnaireViewModel CloneQuestionnaire(QuestionnaireViewModel source) =>
        new()
        {
            Questions = source.Questions
                .ConvertAll(q => new QuestionViewModel
                {
                    Id = q.Id,
                    Index = q.Index,
                    QuestionId = q.QuestionId,
                    SectionSequence = q.SectionSequence,
                    Sequence = q.Sequence,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    DataType = q.DataType,
                    IsMandatory = q.IsMandatory,
                    IsOptional = q.IsOptional,
                    ShowOriginalAnswer = q.ShowOriginalAnswer,
                    Rules = q.Rules
                })
        };
}