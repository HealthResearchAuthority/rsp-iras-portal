using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications;

[Route("/modifications/[action]", Name = "pmc:[action]")]
public class ReviewAllChangesController
(
    IProjectModificationsService projectModificationsService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService,
    IValidator<QuestionnaireViewModel> validator
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

    private readonly ServiceResponse _reviewOutcomeNotFoundError = new()
    {
        StatusCode = System.Net.HttpStatusCode.NotFound,
        Error = "Unable to retrieve modification review outcome details from session."
    };

    [HttpGet]
    public async Task<IActionResult> ReviewAllChanges
#pragma warning disable S107 // Methods should not have too many parameters
        (
        string projectRecordId,
        string irasId,
        string shortTitle,
        Guid projectModificationId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(ProjectOverviewDocumentDto.DocumentType),
        string sortDirection = SortDirections.Ascending
        )
#pragma warning restore S107 // Methods should not have too many parameters
    {
        // Fetch the modification by its identifier
        var (result, modification) = await PrepareModificationAsync(projectModificationId, irasId, shortTitle, projectRecordId);

        if (result is not null)
        {
            return result;
        }

        var sponsorDetailsQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(SponsorDetailsSectionId);

        // get the responent answers for the sponsor details
        var sponsorDetailsResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId);

        var sponsorDetailsAnswers = sponsorDetailsResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var sponsorDetailsQuestionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(sponsorDetailsQuestionsResponse.Content!);

        // Apply answers questions using shared helper
        sponsorDetailsQuestionnaire.UpdateWithRespondentAnswers(sponsorDetailsAnswers);

        modification.SponsorDetails = sponsorDetailsQuestionnaire.Questions;

        // Store the modification details in TempData for later use
        var reviewOutcomeModel = new ReviewOutcomeViewModel
        {
            ModificationDetails = modification,
        };

        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(reviewOutcomeModel);

        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(projectModificationId,
            searchQuery, pageNumber, pageSize, sortField, sortDirection);

        modification.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the questionnaire model containing all questions for the details section.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Locate the question defining "Document Type"
        var documentTypeQuestion = questionnaire.Questions
            .FirstOrDefault(q =>
                string.Equals(q.QuestionId?.ToString(),
                QuestionIds.SelectedDocumentType,
                StringComparison.OrdinalIgnoreCase));

        if (documentTypeQuestion?.Answers?.Any() == true)
        {
            var documentChangeRequest = BuildDocumentRequest();
            // For each document, replace the dropdown value (AnswerId) with the corresponding AnswerText
            foreach (var doc in modification.ProjectOverviewDocumentViewModel.Documents)
            {
                if (!string.IsNullOrWhiteSpace(doc.DocumentType))
                {
                    var matchingAnswer = documentTypeQuestion.Answers
                        .FirstOrDefault(a =>
                            string.Equals(a.AnswerId, doc.DocumentType, StringComparison.OrdinalIgnoreCase));

                    if (matchingAnswer != null)
                    {
                        // Replace the stored AnswerId with the friendly AnswerText
                        doc.DocumentType = matchingAnswer.AnswerText;
                    }
                }

                documentChangeRequest.Id = doc.Id;
                if (!doc.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase))
                {
                    doc.Status = (await EvaluateDocumentCompletion(documentChangeRequest, questionnaire) ? DocumentDetailStatus.Incomplete : DocumentDetailStatus.Completed).ToString();
                }
            }
        }

        modification.ProjectOverviewDocumentViewModel.Pagination = new PaginationViewModel(pageNumber, pageSize, modificationDocumentsResponseResult?.Content?.TotalCount ?? 0)
        {
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "projectdocuments-selection",
            RouteName = "pmc:reviewallchanges",
            AdditionalParameters = new Dictionary<string, string>()
            {
                { "projectRecordId", projectRecordId },
                { "irasId", irasId },
                { "shortTitle", shortTitle },
                { "projectModificationId", projectModificationId.ToString() }
            }
        };

        // Render the details view
        return View(modification);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewOutcome()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        var reviewResponses =
            await projectModificationsService
            .GetModificationReviewResponses(Guid.Parse(model.ModificationDetails.ModificationId!));

        if (reviewResponses.IsSuccessStatusCode && reviewResponses.Content is not null)
        {
            model.ReviewOutcome = reviewResponses.Content.ReviewOutcome;
            model.Comment = reviewResponses.Content.Comment;
            model.ReasonNotApproved = reviewResponses.Content.ReasonNotApproved;
            SaveToTempData(model);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewOutcome(ReviewOutcomeViewModel model, bool saveForLater = false)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        if (!saveForLater && string.IsNullOrEmpty(model.ReviewOutcome))
        {
            ModelState.AddModelError
            (
                nameof(model.ReviewOutcome),
                "You have not selected an outcome. Select a review outcome before you can continue."
            );

            return View(storedModel);
        }

        storedModel.ReviewOutcome = model.ReviewOutcome;
        storedModel.Comment = model.Comment;

        if (model.ReviewOutcome == ModificationStatus.Approved)
        {
            storedModel.ReasonNotApproved = null;
        }

        SaveToTempData(storedModel);

        var saveResponsesResponse = await SaveResponses(storedModel);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Clear();
            return RedirectToAction("Index", "MyTasklist");
        }

        if (model.ReviewOutcome == ModificationStatus.NotApproved)
        {
            return RedirectToAction(nameof(ReasonNotApproved));
        }

        return RedirectToAction(nameof(ConfirmReviewOutcome));
    }

    [HttpGet]
    public IActionResult ReasonNotApproved()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReasonNotApproved(ReviewOutcomeViewModel model, bool saveForLater = false)
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        if (!saveForLater && string.IsNullOrEmpty(model.ReasonNotApproved))
        {
            ModelState.AddModelError
            (
                nameof(model.ReviewOutcome),
                "You have not provided a reason. Enter the reason for modification not being approved before you continue."
            );

            return View(storedModel);
        }

        storedModel.ReasonNotApproved = model.ReasonNotApproved;

        SaveToTempData(storedModel);

        var saveResponsesResponse = await SaveResponses(storedModel);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        if (saveForLater)
        {
            TempData.Clear();
            return RedirectToAction("Index", "MyTasklist");
        }

        return RedirectToAction(nameof(ConfirmReviewOutcome));
    }

    [HttpGet]
    public IActionResult ConfirmReviewOutcome()
    {
        var model = GetFromTempData();

        if (model is null)
        {
            return this.ServiceError(_reviewOutcomeNotFoundError);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitReviewOutcome()
    {
        var storedModel = GetFromTempData() ?? new ReviewOutcomeViewModel();

        var saveResponsesResponse = await SaveResponses(storedModel);

        if (!saveResponsesResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(saveResponsesResponse);
        }

        var modificationId = storedModel.ModificationDetails.ModificationId;

        var newStatus = storedModel.ReviewOutcome;

        var updateResponse = await projectModificationsService.UpdateModificationStatus(Guid.Parse(modificationId!), newStatus!);

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return RedirectToAction(nameof(ReviewOutcomeSubmitted));
    }

    [HttpGet]
    public IActionResult ReviewOutcomeSubmitted()
    {
        TempData.Clear();

        return View();
    }

    public async Task<IActionResult> SendModificationToSponsor(string projectRecordId, Guid projectModificationId)
    {
        // Verify upload success
        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(
            projectModificationId,
            searchQuery, 1, 200,
            nameof(ProjectOverviewDocumentDto.DocumentType),
            SortDirections.Ascending);

        var documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];
        var hasUnfinishedDocuments = documents.Any(d => d.IsMalwareScanSuccessful != true);

        // Verify each document’s detail completeness
        if (!hasUnfinishedDocuments && documents.Any())
        {
            var documentChangeRequest = BuildDocumentRequest();
            var documentStatuses = await GetDocumentCompletionStatuses(documentChangeRequest);
            hasUnfinishedDocuments = documentStatuses.Any(d => d.Status.Equals(DocumentDetailStatus.Incomplete.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        // If any document is unfinished, redirect directly to the UnfinishedChanges view
        if (hasUnfinishedDocuments)
            return RedirectToRoute("pmc:unfinishedchanges");

        // Otherwise, proceed with updating the modification status
        return await HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            ModificationStatus.WithSponsor,
            onSuccess: () => View("ModificationSentToSponsor"));
    }

    private async Task<IActionResult> HandleModificationStatusUpdate(
        string projectRecordId,
        Guid projectModificationId,
        string newStatus,
        Func<IActionResult> onSuccess)
    {
        TempData[TempDataKeys.ProjectRecordId] = projectRecordId;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId;

        var updateResponse = await projectModificationsService.UpdateModificationStatus(projectModificationId, newStatus);

        if (!updateResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(updateResponse);
        }

        return onSuccess();
    }

    private ReviewOutcomeViewModel? GetFromTempData()
    {
        var serializedModel =
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationsDetails) as string;

        if (string.IsNullOrEmpty(serializedModel))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ReviewOutcomeViewModel>(serializedModel);
    }

    private void SaveToTempData(ReviewOutcomeViewModel model)
    {
        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(model);
    }

    private async Task<ServiceResponse> SaveResponses(ReviewOutcomeViewModel model)
    {
        var request = new ProjectModificationReviewRequest
        {
            ProjectModificationId = Guid.Parse(model.ModificationDetails.ModificationId!),
            Outcome = model.ReviewOutcome!,
            Comment = model.Comment,
            ReasonNotApproved = model.ReasonNotApproved
        };

        return await projectModificationsService.SaveModificationReviewResponses(request);
    }

    private async Task<QuestionnaireViewModel> PopulateAnswersFromDocuments(
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
                        .Select(ans => new AnswerViewModel
                        {
                            AnswerId = ans,        // if ans is an ID
                            AnswerText = ans,      // or fetch the display text elsewhere if IDs map to text
                            IsSelected = true
                        })
                        .ToList();
                }
            }
        }

        return questionnaire;
    }
}