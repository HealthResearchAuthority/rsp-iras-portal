using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
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
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService)
{
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;
    private const string DocumentDetailsSection = "pdm-document-metadata";

    [HttpGet]
    public async Task<IActionResult> ReviewAllChanges(string projectRecordId, string irasId, string shortTitle, Guid projectModificationId)
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

        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(projectModificationId,
            searchQuery, 1, 20, nameof(ProjectOverviewDocumentDto.DocumentType), SortDirections.Ascending);

        modification.ProjectOverviewDocumentViewModel.Documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        // Render the details view
        return View(modification);
    }

    [HttpPost]
    public async Task<IActionResult> SendModificationToSponsor(string projectRecordId, Guid projectModificationId)
    {
        var searchQuery = new ProjectOverviewDocumentSearchRequest();
        var modificationDocumentsResponseResult = await projectModificationsService.GetDocumentsForModification(projectModificationId,
            searchQuery, 1, 200, nameof(ProjectOverviewDocumentDto.DocumentType), SortDirections.Ascending);
        var documents = modificationDocumentsResponseResult?.Content?.Documents ?? [];

        // Check if any document is not successfully uploaded
        var hasUnfinishedDocuments = documents.Any(d =>
            !string.Equals(d.Status, DocumentStatus.Success, StringComparison.OrdinalIgnoreCase));

        // If uploads are fine, verify each document’s questionnaire answers
        if (!hasUnfinishedDocuments && documents.Any())
        {
            // Construct the request object containing identifiers required for fetching documents.
            var documentChangeRequest = new ProjectModificationDocumentRequest
            {
                ProjectModificationId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId)!,
                ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
                ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
            };

            // Fetch the CMS question set that defines the metadata/details required for each document.
            var additionalQuestionsResponse = await cmsQuestionsetService
                .GetModificationQuestionSet(DocumentDetailsSection);

            // Build the questionnaire model from the CMS questions.
            var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

            // Call the respondent service to retrieve the list of uploaded documents.
            var response = await respondentService.GetModificationChangesDocuments(
                documentChangeRequest.ProjectModificationId,
                documentChangeRequest.ProjectRecordId,
                documentChangeRequest.ProjectPersonnelId);

            if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
            {
                // For each uploaded document, fetch its associated answers and determine
                // whether details are complete or incomplete.
                var tasks = response.Content
                    .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
                    .Select(async a =>
                    {
                        // Fetch answers already provided for this document.
                        var answersResponse = await respondentService.GetModificationDocumentAnswers(a.Id);
                        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                            ? answersResponse.Content ?? []
                            : [];

                        // Clone the questionnaire to avoid polluting the shared one
                        var clonedQuestionnaire = new QuestionnaireViewModel
                        {
                            Questions = questionnaire.Questions
                                .Select(q => new QuestionViewModel
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
                                .ToList()
                        };

                        clonedQuestionnaire = await PopulateAnswersFromDocuments(clonedQuestionnaire, answers);

                        var isValid = await this.ValidateQuestionnaire(validator, clonedQuestionnaire, true);

                        // Return true if the document is incomplete
                        return !answers.Any() || !isValid;
                    });

                // Wait for all validation tasks and check if any are incomplete
                var results = await Task.WhenAll(tasks);
                hasUnfinishedDocuments = results.Any(r => r);
            }
        }

        // If any document is unfinished, redirect directly to the UnfinishedChanges view
        if (hasUnfinishedDocuments)
        {
            return RedirectToRoute("pmc:unfinishedchanges");
        }

        // Otherwise, proceed with updating the modification status
        return await HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            ModificationStatus.WithSponsor,
            onSuccess: () => View("ModificationSentToSponsor")
        );
    }

    [HttpPost]
    public async Task<IActionResult> SubmitToRegulator(string projectRecordId, Guid projectModificationId, string overallReviewType)
    {
        // Default to WithRegulator if not set or review required
        var statusToSet = ModificationStatus.WithReviewBody;

        // Evaluate the review type (case-insensitive, null-safe)
        if (!string.IsNullOrWhiteSpace(overallReviewType))
        {
            var reviewTypeNormalized = overallReviewType.Trim().ToLowerInvariant();
            statusToSet = reviewTypeNormalized switch
            {
                "no review required" => ModificationStatus.Approved,
                _ => ModificationStatus.WithReviewBody
            };
        }

        // Call your existing handler with the determined status
        return await HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            statusToSet,
            onSuccess: () => RedirectToRoute("pov:projectdetails", new { projectRecordId })
        );
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