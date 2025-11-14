using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
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

    private readonly ServiceResponse _reviewOutcomeNotFoundError = new()
    {
        StatusCode = System.Net.HttpStatusCode.NotFound,
        Error = "Unable to retrieve modification review outcome details from session."
    };

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

        // Store the modification details in TempData for later use
        var reviewOutcomeModel = new ReviewOutcomeViewModel
        {
            ModificationDetails = modification,
        };

        TempData[TempDataKeys.ProjectModification.ProjectModificationsDetails] =
            JsonSerializer.Serialize(reviewOutcomeModel);

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
        // Default to WithReviewBody if not set or review required
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
}