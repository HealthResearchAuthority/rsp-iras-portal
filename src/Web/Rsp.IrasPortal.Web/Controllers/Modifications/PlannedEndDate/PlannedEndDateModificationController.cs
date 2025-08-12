using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
public partial class ProjectModificationController
{
    /// <summary>
    /// Returns the view to update the planned end date of the project.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult PlannedEndDate()
    {
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new PlannedEndDateViewModel
        {
            CurrentPlannedEndDate = TempData.Peek(TempDataKeys.PlannedProjectEndDate) as string ?? string.Empty
        });

        var newPlannedEndDate = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate) as string ?? string.Empty;

        // populate the new saved planned end date if exist
        if (!string.IsNullOrWhiteSpace(newPlannedEndDate))
        {
            var date = DateOnly.ParseExact(newPlannedEndDate, "dd MMMM yyyy", CultureInfo.InvariantCulture);

            viewModel.NewPlannedEndDate.Day = date.ToString("dd");
            viewModel.NewPlannedEndDate.Month = date.ToString("MM");
            viewModel.NewPlannedEndDate.Year = date.ToString("yyyy");
        }

        // Retrieve the current planned end date from TempData
        return View(nameof(PlannedEndDate), viewModel);
    }

    /// <summary>
    /// Handles the POST request to save the new planned end date for a project modification.
    /// Validates the input date, builds the modification answers request, and saves the response.
    /// Returns an error view if validation fails or the original response cannot be found.
    /// </summary>
    /// <param name="model">The view model containing the new planned end date.</param>
    /// <returns>Redirects to the project overview on success, or returns the planned end date view with errors.</returns>
    [HttpPost]
    public async Task<IActionResult> SavePlannedEndDate(PlannedEndDateViewModel model, bool saveForLater = false)
    {
        // Create a validation context for the new planned end date
        var validationContext = new ValidationContext<DateViewModel>(model.NewPlannedEndDate);

        // Override the property name for validation messages
        validationContext.RootContextData[ValidationKeys.PropertyName] = "NewPlannedEndDate.Date";

        // Validate the date if any date component was provided
        if (model.NewPlannedEndDate.Day is not null ||
            model.NewPlannedEndDate.Month is not null ||
            model.NewPlannedEndDate.Year is not null)
        {
            var validationResult = await dateViewModelValidator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                // Return the view with validation errors
                return View(nameof(PlannedEndDate), model);
            }
        }

        // Retrieve previous respondent answers from TempData
        TempData.TryGetValue(TempDataKeys.ProjectRecordResponses, out IEnumerable<RespondentAnswerDto>? projectRecordResponses, true);

        // Find the original response for the planned end date question
        var respondedAnswer = projectRecordResponses?.FirstOrDefault(r => r.QuestionId == QuestionIds.ProjectPlannedEndDate);

        if (respondedAnswer == null)
        {
            // Return an error view if the original response cannot be found
            return View("Error", new ProblemDetails
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                Detail = "Unable to save the new planned end date. Couldn't find the original response",
                Status = StatusCodes.Status400BadRequest,
                Instance = Request.Path
            });
        }

        TempData[TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate] = model.NewPlannedEndDate.Date?.ToString("dd MMMM yyyy");

        var publishedVersion = (TempData.Peek(TempDataKeys.QuestionSetPublishedVersionId) as string)!;

        var isReviewInProgress = TempData.Peek(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges) is true;

        var routeName = (isReviewInProgress, saveForLater) switch
        {
            (true, _) => "pmc:modificationchangesreview",
            (false, false) => "pmc:plannedenddateorganisationtype",
            (false, true) => "pov:projectdetails"
        };

        // Add the new planned end date answer to the request
        return await SaveModificationAnswers([new RespondentAnswerDto
        {
            QuestionId = respondedAnswer.QuestionId,
            VersionId = publishedVersion,
            AnswerText = model.NewPlannedEndDate.Date?.ToString("dd MMMM yyyy"),
            CategoryId = QuestionCategories.PlannedEndDate,
            SectionId = QuestionSections.NewPlannedProjectEndDate
        }], routeName);
    }
}