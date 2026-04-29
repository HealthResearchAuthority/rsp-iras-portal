using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Extensions;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.Modifications.Helpers;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications;

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
    IValidator<QuestionnaireViewModel> validator,
    IFeatureManager featureManager
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

        var modificationReviewResponse = await projectModificationsService.GetModificationReviewResponses(projectRecordId, projectModificationId);

        if (!modificationReviewResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(modificationReviewResponse), null);
        }

        var modificationRfiResponsesResponse = await projectModificationsService.GetModificationRfiResponses(projectRecordId, projectModificationId);

        if (!modificationRfiResponsesResponse.IsSuccessStatusCode)
        {
            return (this.ServiceError(modificationRfiResponsesResponse), null);
        }

        TempData[TempDataKeys.ProjectModification.ProjectModificationStatus] = modification.Status;

        var rfiFeatureFlagEnabled = await featureManager.IsEnabledAsync(FeatureFlags.RequestForInformation);
        string? revisionDescription; string? applicantRevisionResponse;

        if (rfiFeatureFlagEnabled)
        {
            // When RequestForInformation (RFI) is enabled we stop using legacy single
            // RevisionDescription/ApplicantRevisionResponse properties directly.
            //
            // HOWEVER: to preserve backward behaviour for now, we still expose
            // "revisionDescription" as a single aggregated value.
            //
            // The ability to display full response history (multiple revisions)
            // will be implemented in a separate ticket - for some views, most views requires
            // only latest values - so that this properties may remain part of base modification object.

            revisionDescription = null;

            var sponsorResponses = modification?.ModificationRevisionResponses
                .Where(r => r.Role == ResponseRoles.Sponsor);

            var reviseAndAuthorise = sponsorResponses?
                .Where(r => r.ResponseOrigin == ResponseOrigin.ReviseAndAuthorise)
                .OrderByDescending(r => r.CreatedDateTime)
                .FirstOrDefault();

            if (reviseAndAuthorise != null)
            {
                // If a ReviseAndAuthorise response exists, use it as the revision description
                // This mimics the previous behaviour where RevisionDescription was overwritten
                // with the latest sponsor response.
                revisionDescription = reviseAndAuthorise.Response;
            }
            else if (modification?.Status != ModificationStatus.ReviseAndAuthorise)
            {
                // Otherwise, and only if the modification is NOT currently in
                // ReviseAndAuthorise status, fall back to the latest RequestRevisions response.
                revisionDescription = sponsorResponses?
                    .Where(r => r.ResponseOrigin == ResponseOrigin.RequestRevisions)
                    .OrderByDescending(r => r.CreatedDateTime)
                    .FirstOrDefault()?.Response;
            }

            applicantRevisionResponse = modification?.ModificationRevisionResponses
                .Where(r =>
                    r.Role == ResponseRoles.Applicant &&
                    r.ResponseOrigin == ResponseOrigin.RequestRevisions)
                .OrderByDescending(r => r.CreatedDateTime)
                .FirstOrDefault()?.Response;
        }
        else
        {
            // Legacy behaviour when RFI feature flag is disabled:
            // use single-value properties as they were before introducing
            // ModificationRevisionResponses.
            revisionDescription = modification?.RevisionDescription;
            applicantRevisionResponse = modification?.ApplicantRevisionResponse;
        }

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
            DateCreated = DateHelper.ConvertDateToString(modification.CreatedDate),
            DateSponsorSubmittedOutcome = DateHelper.ConvertDateToString(modification.DateSponsorSubmittedOutcome),
            ReasonNotApproved = modification?.ReasonNotApproved ?? string.Empty,
            ReviewerComments = modification?.ReviewerComments,
            RevisionDescription = revisionDescription,
            RfiModel = new RfiDetailsViewModel
            {
                RfiReasons = modificationReviewResponse.Content?.RequestForInformationReasons ?? [],
                RfiResponses = modificationRfiResponsesResponse.Content?.RfiResponses ?? [],
                IsLastSponsorRequestRevisionsDraft = modificationRfiResponsesResponse.Content?.IsLastSponsorRequestRevisionsDraft,
                IsLastSponsorReasonForReviseAndAuthoriseDraft = modificationRfiResponsesResponse.Content?.IsLastSponsorReasonForReviseAndAuthoriseDraft
            },
            ApplicantRevisionResponse = applicantRevisionResponse,
            ModificationRevisionResponses = modification?.ModificationRevisionResponses ?? [],
            HasBeenDuplicated = modification.HasBeenDuplicated,
            DateSubmitted = DateHelper.ConvertDateToString(modification?.SentToRegulatorDate),
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

            TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange?.AutoGeneratedId ?? string.Empty;

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
        TempData[TempDataKeys.ProjectModification.DateCreated] = modification.DateCreated;
        TempData[TempDataKeys.ProjectModification.DateSponsorSubmittedOutcome] = modification.DateSponsorSubmittedOutcome;
        TempData[TempDataKeys.ProjectModification.DateSubmitted] = modification.DateSubmitted;

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
        var documents = response.Content.OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase);

        var documentSummaryItems = new List<DocumentSummaryItemDto>();

        foreach (var doc in documents)
        {
            var summary = await GetDocumentSummary(doc, questionnaire);

            documentSummaryItems.Add(summary);
        }

        return documentSummaryItems;
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

        if (!string.IsNullOrEmpty(a.Status) &&
            !a.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
            (a.Status.Equals(DocumentStatus.Uploaded, StringComparison.OrdinalIgnoreCase) ||
             a.Status.Equals(DocumentStatus.ReviseAndAuthorise, StringComparison.OrdinalIgnoreCase) ||
             a.Status.Equals(DocumentStatus.RequestRevisions, StringComparison.OrdinalIgnoreCase)))
        {
            var isIncomplete = await EvaluateDocumentCompletion(a.Id, questionnaire);
            if (a.Status.Equals(DocumentStatus.ReviseAndAuthorise, StringComparison.OrdinalIgnoreCase)
                    && !isIncomplete)
            {
                status = DocumentStatus.ReviseAndAuthorise;
            }
            else
            {
                status = (isIncomplete
                    ? DocumentDetailStatus.Incomplete
                    : DocumentDetailStatus.Complete).ToString();
            }
        }

        return new DocumentSummaryItemDto
        {
            DocumentId = a.Id,
            FileName = a.LinkedDocumentId != null &&
               string.Equals(a.DocumentType, "Tracked", StringComparison.OrdinalIgnoreCase)
                ? a.FileName
                : $"Add details for {a.FileName}",
            FileSize = a.FileSize ?? 0,
            BlobUri = a.DocumentStoragePath ?? string.Empty,
            Status = status ?? string.Empty,
            LinkedDocumentId = a.LinkedDocumentId,
            DocumentType = a.DocumentType
        };
    }

    /// <summary>
    /// Evaluates whether a single document�s answers are complete.
    /// </summary>
    protected virtual async Task<bool> EvaluateDocumentCompletion(Guid documentId, QuestionnaireViewModel questionnaire, bool addModelErrors = true)
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
        var isValid = await this.ValidateQuestionnaire(
            validator,
            clonedQuestionnaire,
            true,
            addModelErrors
        );

        var supersedeDocumentsEnabled = await featureManager.IsEnabledAsync(FeatureFlags.SupersedingDocuments);

        if (supersedeDocumentsEnabled)
        {
            // Find the "Previous Version of Document" question for superseding logic after saving answers
            var supersedeDocument = clonedQuestionnaire.Questions
            .FirstOrDefault(q =>
                q.QuestionId.Equals(
                    QuestionIds.PreviousVersionOfDocument,
                    StringComparison.OrdinalIgnoreCase))
                    ?.SelectedOption
                    ?.Equals(
                        QuestionIds.PreviousVersionOfDocumentYesOption,
                        StringComparison.OrdinalIgnoreCase)
                    == true;

            // 3. Superseding documents require additional validation
            if (isValid && supersedeDocument)
            {
                return await HasValidSupersedeMetadata(documentId);
            }
        }

        return !answers.Any() || !isValid;
    }

    protected async Task MapDocumentTypesAndStatusesAsync(
      QuestionnaireViewModel questionnaire,
      IEnumerable<ProjectOverviewDocumentDto> documents,
      bool addModelErrors = true,
      bool showIncompleteForReviseAndAuthoriseStatus = false)
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

            // skip validation for incomplete - if document status is ReviseAndAuthorise and its not Review and authorise view
            if (!showIncompleteForReviseAndAuthoriseStatus &&
                doc.Status.Equals(DocumentStatus.ReviseAndAuthorise, StringComparison.OrdinalIgnoreCase))
            {
                doc.Status = DocumentStatus.ReviseAndAuthorise;
                continue;
            }

            if (!showIncompleteForReviseAndAuthoriseStatus &&
                doc.Status.Equals(DocumentStatus.ResponseReviseAndAuthorise, StringComparison.OrdinalIgnoreCase))
            {
                doc.Status = DocumentStatus.ResponseReviseAndAuthorise;
                continue;
            }

            if (!doc.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase) &&
                (doc.Status.Equals(DocumentStatus.Uploaded, StringComparison.OrdinalIgnoreCase) ||
                doc.Status.Equals(DocumentStatus.ReviseAndAuthorise, StringComparison.OrdinalIgnoreCase) ||
                doc.Status.Equals(DocumentStatus.ResponseReviseAndAuthorise, StringComparison.OrdinalIgnoreCase) ||
                doc.Status.Equals(DocumentStatus.RequestRevisions, StringComparison.OrdinalIgnoreCase) ||
                doc.Status.Equals(DocumentStatus.RequestForInformation, StringComparison.OrdinalIgnoreCase)))
            {
                bool isIncomplete = await EvaluateDocumentCompletion(doc.Id, questionnaire, addModelErrors);

                if (doc.Status.Equals(DocumentStatus.ReviseAndAuthorise, StringComparison.OrdinalIgnoreCase)
                    && !isIncomplete)
                {
                    doc.Status = DocumentStatus.ReviseAndAuthorise;
                }
                else if (doc.Status.Equals(DocumentStatus.ResponseReviseAndAuthorise, StringComparison.OrdinalIgnoreCase)
                    && !isIncomplete)
                {
                    doc.Status = DocumentStatus.ResponseReviseAndAuthorise;
                }
                else
                {
                    doc.Status = (isIncomplete
                        ? DocumentDetailStatus.Incomplete
                        : DocumentDetailStatus.Complete).ToString();
                }
            }
        }
    }

    protected async Task<List<ModificationChangeModel>> UpdateModificationChanges(string projectRecordId, List<ModificationChangeModel> modificationChanges)
    {
        const string OrganisationDetailsSection = "pom-participating-organisation-details";

        foreach (var modificationChange in modificationChanges)
        {
            // get the responent answers for the category
            var getModificationChangeAnswersResponse = await respondentService.GetModificationChangeAnswers(modificationChange.ModificationChangeId, projectRecordId);

            // get the questions for the modification journey
            var questionSetServiceResponse = await cmsQuestionsetService.GetModificationsJourney(modificationChange.SpecificAreaOfChangeId);

            // return the error view if unsuccessfull
            if (!getModificationChangeAnswersResponse.IsSuccessStatusCode || !questionSetServiceResponse.IsSuccessStatusCode)
            {
                // return the modificationChanges unchanged in case of error
                return modificationChanges;
            }

            // we need to validate the participating organisations for AddNewSites
            // questions separately for each participating organisation, so remove from the main questionnaire
            if
            (
                modificationChange.SpecificAreaOfChangeId is
                    SpecificAreasOfChange.AddNewPics or
                    SpecificAreasOfChange.AddNewSites or
                    SpecificAreasOfChange.EarlyClosureSites or
                    SpecificAreasOfChange.EarlyClosuresPics
            )
            {
                var sections = questionSetServiceResponse.Content!.Sections.ToList();

                sections.RemoveAll(s => s.Id == OrganisationDetailsSection);

                questionSetServiceResponse.Content.Sections = sections;
            }

            var isParticipatingOrgsQuestionnaireValid = true;

            // cater for participating organisations questions
            if (modificationChange.SpecificAreaOfChangeId is SpecificAreasOfChange.AddNewSites)
            {
                var getParticipatingOrganisationsResponse = await respondentService.GetModificationParticipatingOrganisations(modificationChange.ModificationChangeId, projectRecordId);
                var orgsQuestionSet = await cmsQuestionsetService.GetModificationQuestionSet(OrganisationDetailsSection);

                var questionIndex = 0;

                foreach (var organisation in getParticipatingOrganisationsResponse.Content!)
                {
                    var questionnaireViewModel = QuestionsetHelpers.BuildQuestionnaireViewModel(orgsQuestionSet.Content!);

                    var participatingOrganisationQuestionnaire = questionnaireViewModel;

                    var answersResponse = await respondentService.GetModificationParticipatingOrganisationAnswers(organisation.Id);

                    var answers = answersResponse.Content ?? [];

                    participatingOrganisationQuestionnaire.Questions = questionnaireViewModel.Questions.ConvertAll(cmsQ =>
                    {
                        var matchingAnswer = answers.FirstOrDefault(a => a.QuestionId == cmsQ.QuestionId);

                        cmsQ.Index = questionIndex++;

                        if (matchingAnswer == null)
                        {
                            return cmsQ;
                        }

                        cmsQ.Id = matchingAnswer.Id;

                        return cmsQ;
                    });

                    participatingOrganisationQuestionnaire.UpdateWithRespondentAnswers(answers);

                    isParticipatingOrgsQuestionnaireValid = await this.ValidateQuestionnaire(validator, participatingOrganisationQuestionnaire, true, false);

                    if (!isParticipatingOrgsQuestionnaireValid)
                    {
                        break;
                    }
                }
            }

            var respondentAnswers = getModificationChangeAnswersResponse.Content!;

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

            modificationChange.ChangeStatus = result && isParticipatingOrgsQuestionnaireValid ?
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
        string? sortDirection,
        bool isSponsorRevisingModification = false)
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

        if (isSponsorRevisingModification)
        {
            // Upewnij się, że lista nie jest null
            searchQuery.AllowedStatuses ??= new List<string>();

            var toAdd = new[]
            {
                    DocumentStatus.Uploaded,
                    DocumentStatus.Failed,
                    DocumentStatus.Incomplete,
                    DocumentStatus.Complete,
                    DocumentStatus.RequestForInformation
            };

            foreach (var status in toAdd)
            {
                if (!searchQuery.AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                {
                    searchQuery.AllowedStatuses.Add(status);
                }
            }
        }

        // Fetch documents
        var documentsForModification = await projectModificationsService.GetDocumentsForModification(
            projectModificationId,
            searchQuery,
            pageNumber,
            pageSize,
            sortField,
            sortDirection
        );

        return (documentsForModification, questionnaire);
    }

    protected static IEnumerable<ProjectOverviewDocumentDto> GetSortedAndPaginatedDocuments(
        IEnumerable<ProjectOverviewDocumentDto> allDocuments,
        string sortField,
        string sortDirection,
        int pageSize,
        int pageNumber)
    {
        // do sorting of status field here because status field is mapped and not stored in DB
        if (sortField == nameof(ProjectOverviewDocumentDto.Status))
        {
            if (sortDirection == SortDirections.Ascending)
            {
                allDocuments = allDocuments.OrderBy(d => d.Status);
            }
            else
            {
                allDocuments = allDocuments.OrderByDescending(d => d.Status);
            }
        }

        return allDocuments.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
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

    private async Task<bool> HasValidSupersedeMetadata(Guid documentId)
    {
        var response = await respondentService.GetModificationDocumentDetails(documentId);

        if (response?.StatusCode != HttpStatusCode.OK || response.Content == null)
            return false;

        var document = response.Content;

        var hasReplaces = document.ReplacesDocumentId.HasValue;
        var hasLinked = document.LinkedDocumentId.HasValue && document.LinkedDocumentId != Guid.Empty;
        var hasType = !string.IsNullOrWhiteSpace(document.DocumentType);

        if (!hasReplaces && hasType && string.Equals(document.DocumentType, "Clean", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (hasType && hasLinked)
        {
            return false;
        }

        // All other combinations are valid
        return true;
    }
}