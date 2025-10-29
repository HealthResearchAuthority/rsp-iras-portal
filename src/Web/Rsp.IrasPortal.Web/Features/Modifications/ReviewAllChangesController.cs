using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;

namespace Rsp.IrasPortal.Web.Features.Modifications;

[Route("/modifications/[action]", Name = "pmc:[action]")]
public class ReviewAllChangesController
(
    IProjectModificationsService projectModificationsService,
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService
) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService)
{
    private const string SponsorDetailsSectionId = "pm-sponsor-reference";
    private readonly IRespondentService _respondentService = respondentService;

    [HttpGet]
    public async Task<IActionResult> ReviewAllChanges(string projectRecordId, string irasId, string shortTitle, Guid projectModificationId)
    {
        // Fetch the modification by its identifier
        var (modificationResult, model) = await GetModificationDetails(projectModificationId, irasId, shortTitle, projectRecordId);

        // Short-circuit with a service error if the call failed
        if (modificationResult is not null)
        {
            return modificationResult;
        }

        var modification = model!;

        // Persist the modification identifier in TempData for subsequent requests/pages
        TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modification.ModificationIdentifier;
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = modification.ModificationId;

        var (changesResult, initialQuestions, modificationChanges) = await GetModificationChanges(modification);

        if (changesResult is not null)
        {
            return changesResult;
        }

        // populate all the answers for the changes questions,
        // calculates the ranking for each change and adds the change
        // to the modification model.
        await UpdateModificationWithChanges(initialQuestions!, modification, modificationChanges!);

        // overall modification ranking
        modification.UpdateOverAllRanking();
        TempData[TempDataKeys.ProjectModification.OverallReviewType] = modification.ReviewType;

        var sponsorDetailsQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(SponsorDetailsSectionId);

        // get the responent answers for the sponsor details
        var sponsorDetailsResponse = await _respondentService.GetModificationAnswers(projectModificationId, projectRecordId);

        var sponsorDetailsAnswers = sponsorDetailsResponse.Content!;

        // convert the questions response to QuestionnaireViewModel
        var sponsorDetailsQuestionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(sponsorDetailsQuestionsResponse.Content!);

        // Apply answers questions using shared helper
        sponsorDetailsQuestionnaire.UpdateWithRespondentAnswers(sponsorDetailsAnswers);

        modification.SponsorDetails = sponsorDetailsQuestionnaire.Questions;

        // Render the details view
        return View(modification);
    }

    [HttpPost]
    public Task<IActionResult> SendModificationToSponsor(string projectRecordId, Guid projectModificationId)
    {
        return HandleModificationStatusUpdate(
            projectRecordId,
            projectModificationId,
            ModificationStatus.WithSponsor,
            onSuccess: () => View("ModificationSentToSponsor")
        );
    }

    [HttpPost]
    public Task<IActionResult> SubmitToRegulator(string projectRecordId, Guid projectModificationId, string overallReviewType)
    {
        // Default to WithRegulator if not set or review required
        var statusToSet = ModificationStatus.WithRegulator;

        // Evaluate the review type (case-insensitive, null-safe)
        if (!string.IsNullOrWhiteSpace(overallReviewType))
        {
            statusToSet = overallReviewType switch
            {
                "no review required" => ModificationStatus.Approved,
                _ => ModificationStatus.WithRegulator
            };
        }

        // Call your existing handler with the determined status
        return HandleModificationStatusUpdate(
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