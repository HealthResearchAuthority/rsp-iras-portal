using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
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
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

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

    [HttpPost]
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
        var hasUnfinishedDocuments = documents.Any(d =>
            !string.Equals(d.Status, DocumentStatus.Success, StringComparison.OrdinalIgnoreCase));

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
}