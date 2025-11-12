using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
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
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

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