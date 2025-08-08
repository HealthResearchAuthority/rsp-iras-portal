using System.Globalization;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsUser")]
public class ApplicationController
(
    IApplicationsService applicationsService,
    IValidator<IrasIdViewModel> irasIdValidator,
    IRespondentService respondentService,
    IQuestionSetService questionSetService) : Controller
{
    // ApplicationInfo view name
    private const string ApplicationInfo = nameof(ApplicationInfo);

    public async Task<IActionResult> Welcome(
        string? searchQuery = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ApplicationModel.CreatedDate),
        string? sortDirection = SortDirections.Descending)
    {
        var model = new ApplicationsViewModel();

        // getting respondentID from Http context
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // getting research applications by respondent ID
        var applicationServiceResponse = await applicationsService.GetPaginatedApplicationsByRespondent(respondentId, searchQuery, pageNumber, pageSize, sortField, sortDirection);

        model.Applications = applicationServiceResponse.Content?.Items
            .Select(dto => new ApplicationModel
            {
                Id = dto.Id,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Project title" : dto.Title,
                Status = dto.Status,
                CreatedDate = dto.CreatedDate,
                IrasId = dto.IrasId
            })
            .ToList() ?? [];

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, applicationServiceResponse.Content?.TotalCount ?? 0)
        {
            RouteName = "app:welcome",
            SearchQuery = searchQuery,
            SortDirection = sortDirection,
            SortField = sortField,
            FormName = "applications-selection"
        };

        return View(nameof(Index), model);
    }

    public IActionResult StartProject() => View(nameof(StartProject));

    /// <summary>
    /// Handles the POST request to start a new project.
    /// Validates the IRAS ID, checks for duplicates, creates a new application, and redirects to the questionnaire.
    /// </summary>
    /// <param name="model">The IRAS ID view model containing the IRAS ID input by the user.</param>
    /// <returns>
    /// If validation fails or a duplicate IRAS ID is found, returns the StartProject view with errors.
    /// If the application is created successfully, redirects to the Questionnaire Resume action.
    /// Otherwise, returns a service error view.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> StartProject(IrasIdViewModel model)
    {
        // Clear session and TempData to ensure a clean state for the new project
        HttpContext.Session.Clear();
        TempData.Clear();

        // Validate the IRAS ID input
        var validationResult = await irasIdValidator.ValidateAsync(new ValidationContext<IrasIdViewModel>(model));

        if (!validationResult.IsValid)
        {
            // Add validation errors to the ModelState
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            // Return the view with validation errors
            return View(nameof(StartProject), model);
        }

        // Retrieve all existing applications
        var applicationsResponse = await applicationsService.GetApplications();
        if (!applicationsResponse.IsSuccessStatusCode || applicationsResponse.Content == null)
        {
            // Return a generic service error view if retrieval fails
            return this.ServiceError(applicationsResponse);
        }

        // Check if an application with the same IRAS ID already exists
        bool irasIdExists = applicationsResponse.Content.Any(app => app.IrasId?.ToString() == model.IrasId);
        if (irasIdExists)
        {
            // Add a model error for duplicate IRAS ID
            ModelState.AddModelError(nameof(model.IrasId), "A record for the project with this IRAS ID already exists");
            return View(nameof(StartProject), model);
        }

        // Get the respondent information from the current context
        var respondent = this.GetRespondentFromContext();
        var name = $"{respondent.GivenName} {respondent.FamilyName}";

        // Create a new application request object
        var irasApplicationRequest = new IrasApplicationRequest
        {
            Title = string.Empty,
            Description = string.Empty,
            CreatedBy = name,
            UpdatedBy = name,
            StartDate = DateTime.Now,
            Respondent = respondent,
            IrasId = model.IrasId != null ? int.Parse(model.IrasId) : null,
        };

        // Call the service to create the new application
        var createResponse = await applicationsService.CreateApplication(irasApplicationRequest);
        if (!createResponse.IsSuccessStatusCode || createResponse.Content == null)
        {
            // Return a generic service error view if creation fails
            return this.ServiceError(createResponse);
        }

        var irasApplication = createResponse.Content;
        // Save the newly created application to the session
        HttpContext.Session.SetString(SessionKeys.ProjectRecord, JsonSerializer.Serialize(irasApplication));

        // Store relevant information in TempData for use in subsequent requests
        TempData[TempDataKeys.CategoryId] = QuestionCategories.ProjectRecrod;
        TempData[TempDataKeys.ProjectRecordId] = irasApplication.Id;
        TempData[TempDataKeys.IrasId] = irasApplication.IrasId;

        // Redirect to the Questionnaire Resume action to continue the application process
        return RedirectToAction(nameof(QuestionnaireController.Resume), "Questionnaire", new
        {
            categoryId = QuestionCategories.ProjectRecrod,
            projectRecordId = irasApplication.Id
        });
    }

    public IActionResult CreateApplication() => View(nameof(CreateApplication));

    public IActionResult DocumentUpload()
    {
        TempData.TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var documents, true);

        return View(documents);
    }

    [HttpPost]
    public IActionResult Upload(IFormFileCollection formFiles)
    {
        List<Document> documents = [];

        foreach (var file in formFiles)
        {
            documents.Add(new Document
            {
                Name = file.FileName,
                Size = file.Length,
                Type = Path.GetExtension(file.FileName)
            });
        }

        TempData.TryAdd(TempDataKeys.UploadedDocuments, documents, true);

        return RedirectToAction(nameof(DocumentUpload));
    }

    [HttpGet]
    [FeatureGate(Features.MyApplications)]
    public async Task<IActionResult> MyApplications()
    {
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        var avm = new ApplicationsViewModel();

        HttpContext.Session.RemoveAllSessionValues();

        // get the pending applications
        var applicationServiceResponse = await applicationsService.GetApplicationsByRespondent(respondentId);

        // return the view if successful
        if (applicationServiceResponse is { IsSuccessStatusCode: true, Content: not null })
        {
            avm.Applications = applicationServiceResponse.Content
            .Select(dto => new ApplicationModel
            {
                Id = dto.Id,
                Title = dto.Title,
                Status = dto.Status,
                CreatedDate = dto.CreatedDate,
                IrasId = dto.IrasId,
                Description = dto.Description,
                CreatedBy = dto.CreatedBy
            })
            .ToList();

            var questionSetServiceResponse = await questionSetService.GetQuestionCategories();

            if (questionSetServiceResponse is { IsSuccessStatusCode: true, Content: not null })
            {
                avm.Categories = questionSetServiceResponse.Content;
                return View(avm);
            }

            // return the generic error page
            return this.ServiceError(questionSetServiceResponse);
        }

        // return the generic error page
        return this.ServiceError(applicationServiceResponse);
    }

    /// <summary>
    /// Displays the project overview page. Shows a notification banner if a project modification change exists,
    /// clears related TempData keys, and populates the ProjectOverviewModel with project details from TempData.
    /// </summary>
    /// <returns>The ProjectOverview view with the populated model.</returns>
    public async Task<IActionResult> ProjectOverview(string? projectRecordId, string? categoryId)
    {
        // If there is a project modification change, show the notification banner
        if (TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is not null)
        {
            TempData[TempDataKeys.ShowNotificationBanner] = true;
        }

        // Remove modification-related TempData keys to reset state
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationIdentifier);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.ProjectModificationSpecificArea);
        TempData.Remove(TempDataKeys.ProjectModification.AreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModification.SpecificAreaOfChangeId);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources);
        TempData.Remove(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges);
        TempData.Remove(TempDataKeys.QuestionSetPublishedVersionId);

        // Indicate that the project overview is being shown
        TempData[TempDataKeys.ProjectOverview] = true;

        if (projectRecordId is not null && categoryId is not null)
        {
            return await GetProjectOverview(projectRecordId, categoryId);
        }

        // Build the model using values from TempData, falling back to defaults if not present
        var model = new ProjectOverviewModel
        {
            ProjectTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecrod, //TempData.Peek(TempDataKeys.CategoryId) as string ?? string.Empty,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPlannedEndDate = TempData.Peek(TempDataKeys.PlannedProjectEndDate) as string ?? string.Empty
        };

        // Get all respondent answers for the project and category
        var respondentAnswersResponse = await respondentService.GetRespondentAnswers(model.ProjectRecordId, model.CategoryId);

        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(respondentAnswersResponse);
        }

        var answers = respondentAnswersResponse.Content;

        if (answers == null)
        {
            // Return a 404 error view if no responses are found for the project record
            return View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "No responses found for the project record",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            });
        }

        TempData.TryAdd(TempDataKeys.ProjectRecordResponses, answers, true);

        return View(model);
    }

    public IActionResult ReviewAnswers()
    {
        return View(this.GetApplicationFromSession());
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ProblemDetails());
    }

    /// <summary>
    /// Retrieves and displays the project overview for a given project record and category.
    /// Fetches the project record and respondent answers, populates TempData with key details,
    /// and returns the ProjectOverview view with a populated model.
    /// </summary>
    /// <param name="projectRecordId">The unique identifier for the project record.</param>
    /// <param name="categoryId">The unique identifier for the question category.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> that renders the ProjectOverview view with the project details,
    /// or an error view if the project record or answers are not found or a service error occurs.
    /// </returns>
    [NonAction]
    public async Task<IActionResult> GetProjectOverview(string projectRecordId, string categoryId)
    {
        // Retrieve the project record by its ID
        var projectRecordResponse = await applicationsService.GetProjectRecord(projectRecordId);

        // If the service call failed, return a generic service error view
        if (!projectRecordResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(projectRecordResponse);
        }

        var projectRecord = projectRecordResponse.Content;

        if (projectRecord == null)
        {
            // Return a 404 error view if the project record is not found
            return View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "Project record not found",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            });
        }

        // Get all respondent answers for the project and category
        var respondentAnswersResponse = await respondentService.GetRespondentAnswers(projectRecordId, categoryId);

        if (!respondentAnswersResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(respondentAnswersResponse);
        }

        var answers = respondentAnswersResponse.Content;

        if (answers == null)
        {
            // Return a 404 error view if no responses are found for the project record
            return View("Error", new ProblemDetails()
            {
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound),
                Detail = "No responses found for the project record",
                Status = StatusCodes.Status404NotFound,
                Instance = Request.Path
            });
        }

        TempData.TryAdd(TempDataKeys.ProjectRecordResponses, answers, true);

        // Extract key answers from the respondent answers
        var titleAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ShortProjectTitle)?.AnswerText;
        var endDateAnswer = answers.FirstOrDefault(a => a.QuestionId == QuestionIds.ProjectPlannedEndDate)?.AnswerText;

        // Populate TempData with project details
        TempData[TempDataKeys.IrasId] = projectRecord.IrasId;
        TempData[TempDataKeys.ProjectRecordId] = projectRecord.Id;

        if (!string.IsNullOrWhiteSpace(titleAnswer))
        {
            TempData[TempDataKeys.ShortProjectTitle] = titleAnswer;
        }

        var ukCulture = new CultureInfo("en-GB");
        if (DateTime.TryParse(endDateAnswer, ukCulture, DateTimeStyles.None, out var parsedDate))
        {
            TempData[TempDataKeys.PlannedProjectEndDate] = parsedDate.ToString("dd MMMM yyyy");
        }

        // Build the model using values from TempData, falling back to defaults if not present
        var model = new ProjectOverviewModel
        {
            ProjectTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            CategoryId = QuestionCategories.ProjectRecrod,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPlannedEndDate = TempData.Peek(TempDataKeys.PlannedProjectEndDate) as string ?? string.Empty
        };

        return View("ProjectOverview", model);
    }
}