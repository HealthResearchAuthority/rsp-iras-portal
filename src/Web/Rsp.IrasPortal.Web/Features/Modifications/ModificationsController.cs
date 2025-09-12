using System.Data;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.IrasPortal.Web.Features.Modifications;

/// <summary>
/// Controller responsible for handling project modification related actions.
/// </summary>
[Route("[controller]/[action]", Name = "pmc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ModificationsController
(
    IProjectModificationsService projectModificationsService,
    IRespondentService respondentService,
    ICmsQuestionsetService cmsQuestionsetService,
    IValidator<AreaOfChangeViewModel> areaofChangeValidator
) : Controller

{
    private const string SelectAreaOfChange = "Select area of change";
    private const string SelectSpecificAreaOfChange = "Select specific change";

    /// <summary>
    /// Initiates the creation of a new project modification.
    /// </summary>
    /// <param name="separator">Separator to use in the modification identifier. Default is "/".</param>
    /// <returns>Redirects to the resume route if successful, otherwise returns an error page.</returns>
    [HttpGet]
    public async Task<IActionResult> CreateModification(string separator = "/")
    {
        // Retrieve IRAS ID from TempData
        var IrasId = TempData.Peek(TempDataKeys.IrasId) as int?;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId);

        // Check if required TempData values are present
        if (string.IsNullOrEmpty((string?)projectRecordId) || IrasId == null)
        {
            // Return a problem response if data is missing
            var problemDetails = this.ProblemResult(new ServiceResponse
            {
                Error = "ProjectRecordId and/or IrasId missing",
                StatusCode = HttpStatusCode.BadRequest,
                ReasonPhrase = "Bad Request"
            });

            return Problem(problemDetails.Detail, problemDetails.Instance, problemDetails.Status, problemDetails.Title, problemDetails.Type);
        }

        // Get respondent information from the current context
        var respondent = this.GetRespondentFromContext();

        // Compose the full name of the respondent
        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new project modification request
        var modificationRequest = new ProjectModificationRequest
        {
            ProjectRecordId = (string)projectRecordId,
            ModificationIdentifier = IrasId + separator,
            Status = "OPEN",
            CreatedBy = name,
            UpdatedBy = name
        };

        // Call the service to create the modification
        var projectModificationServiceResponse = await projectModificationsService.CreateModification(modificationRequest);

        // If the service call failed, return a generic error page
        if (!projectModificationServiceResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectModificationServiceResponse);
        }

        // Retrieve the created project modification from the response
        var projectModification = projectModificationServiceResponse.Content!;

        // Store relevant IDs in TempData for later use
        TempData[TempDataKeys.ProjectModification.ProjectModificationId] = projectModification.Id;
        TempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = projectModification.ModificationIdentifier;
        TempData[TempDataKeys.CategoryId] = QuestionCategories.ProjectModification;

        return RedirectToAction(nameof(AreaOfChange));
    }

    /// <summary>
    /// Displays the Area of Change selection screen.
    /// Retrieves area of change data and stores it in session for later reuse.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AreaOfChange()
    {
        var questionSetResponse = await cmsQuestionsetService.GetModificationQuestionSet();

        // a published version of question set must exist
        // so that it can be used when saving the modification/changes
        if (!questionSetResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(questionSetResponse);
        }

        var questionSet = questionSetResponse.Content!;

        var publishedVersion = questionSet.Version;

        if (publishedVersion == null)
        {
            return this.ServiceError(questionSetResponse);
        }

        TempData[TempDataKeys.QuestionSetPublishedVersionId] = publishedVersion;

        // get the initial modification questions from CMS
        var startingQuestionsResponse = await cmsQuestionsetService.GetInitialModificationQuestions();

        if (!startingQuestionsResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(startingQuestionsResponse);
        }

        var startingQuestions = startingQuestionsResponse.Content!;

        // Handle case when no area of change data is returned from service
        if (startingQuestions == null)
        {
            return View(new AreaOfChangeViewModel
            {
                AreaOfChangeOptions =
                [
                    new() { Text = SelectAreaOfChange, Value = Guid.Empty.ToString() }
                ],
                SpecificChangeOptions =
                [
                    new() { Text = SelectSpecificAreaOfChange, Value = Guid.Empty.ToString() }
                ]
            });
        }

        // Store the list of area of changes in TempData (as serialized JSON string)
        TempData[TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(startingQuestions.AreasOfChange);

        //Set the modification change marker to an empty GUID only if no change has been applied yet.
        if (!(TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] is Guid))
        {
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.Empty;
        }

        var viewModel = TempData.PopulateBaseProjectModificationProperties(new AreaOfChangeViewModel
        {
            AreaOfChangeOptions = startingQuestions.AreasOfChange
                .Select(a => new SelectListItem { Value = a.AutoGeneratedId, Text = a.OptionName })
                .Prepend(new SelectListItem { Value = Guid.Empty.ToString(), Text = SelectAreaOfChange }),
            SpecificChangeOptions = [new SelectListItem { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange }],
        });

        viewModel.SpecificAreaOfChange = SelectAreaOfChange;

        // Populate the dropdown options based on any existing selections
        return View("AreaOfChange", viewModel);
    }

    /// <summary>
    /// Returns the specific changes related to a selected Area of Change.
    /// Pulls data from session cache for performance.
    /// </summary>
    [HttpGet]
    public IActionResult GetSpecificChangesByAreaId(string areaOfChangeId)
    {
        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (string.IsNullOrWhiteSpace(tempDataString))
        {
            return BadRequest("Area of changes not available.");
        }

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;

        // Find the specific area of change based on the provided ID
        var selectedArea = areaOfChanges.FirstOrDefault(a => a.AutoGeneratedId == areaOfChangeId);

        // If no area is found, return an empty list
        var specificChanges = selectedArea?.SpecificAreasOfChange ?? [];

        // Create a SelectListItem list for the specific changes
        var selectList = new List<SelectListItem> { new() { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange } };

        // Add each specific change to the SelectListItem list
        selectList.AddRange(specificChanges.Select(sc => new SelectListItem
        {
            Value = sc.AutoGeneratedId,
            Text = sc.OptionName
        }));

        // Return the list as a JSON result
        return Json(selectList);
    }

    /// <summary>
    /// Processes user’s selection of Area and Specific Change and redirects based on journey type.
    /// Saves the modification change to backend and handles validation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ConfirmModificationJourney(AreaOfChangeViewModel model, string action)
    {
        if (action == "saveAndContinue")
        {
            var validationResult = await areaofChangeValidator.ValidateAsync(new ValidationContext<AreaOfChangeViewModel>(model));

            if (!validationResult.IsValid)
            {
                HandleValidationErrors(validationResult, model);
                return View(nameof(AreaOfChange), model);
            }
        }

        var modificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);

        if (modificationId is Guid newGuid && newGuid != Guid.Empty)
        {
            await SaveModificationChange(model, newGuid);

            TempData[TempDataKeys.ProjectModification.AreaOfChangeId] = model.AreaOfChangeId;
            TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = model.SpecificChangeId;
        }

        if (action == "saveForLater")
        {
            return RedirectToRoute("pov:postapproval", new { model.ProjectRecordId });
        }

        var areas = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        // Deserialize the area of changes from TempData
        var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(areas!)!;

        // Find the change based on the selected area and specific change IDs
        var areaOfChange = areaOfChanges
            .First(a => a.AutoGeneratedId == model.AreaOfChangeId);

        // Find the specific change based on the selected area and specific change IDs
        var selectedChange = areaOfChange.SpecificAreasOfChange
            .First(sc => sc.AutoGeneratedId == model.SpecificChangeId);

        // get the modification journey from cms for the
        // sepcific area of change
        var modificationJourney = await cmsQuestionsetService.GetModificationsJourney(selectedChange.AutoGeneratedId!);

        // if we have sections then grab the first section
        var sections = modificationJourney.Content?.Sections ?? [];

        // section shouldn't be null here, this is a defensive
        // check
        if (sections is { Count: 0 } || string.IsNullOrEmpty(sections[0].StaticViewName))
        {
            return this.ServiceError(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = $"Unable to load questionnaire for the selected specific area of change: {selectedChange.OptionName}",
            });
        }

        var section = sections[0];

        model.ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId)?.ToString() ?? string.Empty;

        TempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = selectedChange.OptionName;

        return RedirectToRoute($"pmc:{section.StaticViewName}", new
        {
            projectRecordId = model.ProjectRecordId,
            categoryId = section.CategoryId,
            sectionId = section.Id,
        });
    }

    /// <summary>
    /// Adds validation errors to ModelState and rebuilds dropdowns from session.
    /// </summary>
    private void HandleValidationErrors(ValidationResult validationResult, AreaOfChangeViewModel model)
    {
        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        var tempDataString = TempData.Peek(TempDataKeys.ProjectModification.AreaOfChanges) as string;

        if (!string.IsNullOrWhiteSpace(tempDataString))
        {
            var areaOfChanges = JsonSerializer.Deserialize<List<AreaOfChangeDto>>(tempDataString)!;
            PopulateDropdownOptions(model, areaOfChanges);
        }
    }

    /// <summary>
    /// Populates AreaOfChange and SpecificChange dropdowns based on current selection.
    /// </summary>
    private static void PopulateDropdownOptions(AreaOfChangeViewModel model, List<AreaOfChangeDto> areaOfChanges)
    {
        model.AreaOfChangeOptions = areaOfChanges
            .Select(a => new SelectListItem
            {
                Value = a.AutoGeneratedId,
                Text = a.OptionName,
                Selected = a.AutoGeneratedId == model.AreaOfChangeId
            })
            .Prepend(new SelectListItem { Value = Guid.Empty.ToString(), Text = SelectAreaOfChange });

        if (model.AreaOfChangeId != null)
        {
            var selectedArea = areaOfChanges.FirstOrDefault(a => a.Id == model.AreaOfChangeId);
            model.SpecificChangeOptions = selectedArea?.SpecificAreasOfChange?
                .Select(sc => new SelectListItem
                {
                    Value = sc.AutoGeneratedId,
                    Text = sc.OptionName,
                    Selected = sc.AutoGeneratedId == model.SpecificChangeId
                })
                .Prepend(new SelectListItem { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange })
                ?? [new() { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange }];
        }
        else
        {
            model.SpecificChangeOptions = [new SelectListItem { Value = Guid.Empty.ToString(), Text = SelectSpecificAreaOfChange }];
        }
    }

    [NonAction]
    public async Task<IActionResult> SaveModificationAnswers(List<RespondentAnswerDto> respondentAnswers, string routeName)
    {
        // Get required modification data for saving the planned end date
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Build the request to save modification answers
        var request = new ProjectModificationAnswersRequest
        {
            ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId
        };

        // Add the new planned end date answer to the request
        request.ModificationAnswers.AddRange(respondentAnswers);

        // Save the modification answers using the respondent service
        var saveModificationAnswersResponse = await respondentService.SaveModificationAnswers(request);

        if (!saveModificationAnswersResponse.IsSuccessStatusCode)
        {
            // Return a service error view if saving fails
            return this.ServiceError(saveModificationAnswersResponse);
        }

        if (routeName == "pov:postapproval")
        {
            return RedirectToRoute(routeName, new { projectRecordId });
        }

        return RedirectToRoute(routeName);
    }

    /// <summary>
    /// Saves a new ProjectModificationChange record to the backend based on user selection.
    /// </summary>
    private async Task SaveModificationChange(AreaOfChangeViewModel model, Guid modificationId)
    {
        var respondent = this.GetRespondentFromContext();

        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new ProjectModificationChangeRequest
        var modificationChangeResponse = await projectModificationsService.CreateModificationChange(new ProjectModificationChangeRequest
        {
            AreaOfChange = model.AreaOfChangeId!,
            SpecificAreaOfChange = model.SpecificChangeId!,
            ProjectModificationId = modificationId,
            Status = "OPEN",
            CreatedBy = name,
            UpdatedBy = name
        });

        // Store the modification change ID in TempData for later use
        if (modificationChangeResponse.IsSuccessStatusCode)
        {
            var modificationChange = modificationChangeResponse.Content!;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChange.Id;
            TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();
        }
    }
}