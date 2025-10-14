using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ApplicationController
(
    IApplicationsService applicationsService,
    IValidator<IrasIdViewModel> irasIdValidator,
    ICmsQuestionSetServiceClient cmsSevice,
    IValidator<ApplicationSearchModel> searchValidator) : Controller
{
    // ApplicationInfo view name
    private const string ApplicationInfo = nameof(ApplicationInfo);

    public async Task<IActionResult> Welcome(
        int pageNumber = 1,
        int pageSize = 20,
        string? sortField = nameof(ApplicationModel.CreatedDate),
        string? sortDirection = SortDirections.Descending)
    {
        var model = new ApplicationsViewModel
        {
            EmptySearchPerformed = true, // Set to true to check if search bar should be hidden on view
        };

        // getting respondentID from Http context
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // getting search query
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (!string.IsNullOrEmpty(json))
        {
            model.Search = JsonSerializer.Deserialize<ApplicationSearchModel>(json)!;
            if (model.Search?.Filters?.Count != 0 || !string.IsNullOrEmpty(model.Search.SearchTitleTerm))
            {
                model.EmptySearchPerformed = false;
            }
        }

        var searchQuery = new ApplicationSearchRequest()
        {
            SearchTitleTerm = model.Search.SearchTitleTerm,
            Status = model.Search.Status,
            FromDate = model.Search.FromDate,
            ToDate = model.Search.ToDate,
        };

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
    [CmsContentAction(nameof(StartProject))]
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
        var questionCategoriesResponse = await cmsSevice.GetQuestionCategories();
        var categoryId = questionCategoriesResponse.IsSuccessStatusCode && questionCategoriesResponse.Content?.FirstOrDefault() != null
            ? questionCategoriesResponse.Content.FirstOrDefault()?.CategoryId : QuestionCategories.ProjectRecrod;

        TempData[TempDataKeys.CategoryId] = categoryId;
        TempData[TempDataKeys.ProjectRecordId] = irasApplication.Id;
        TempData[TempDataKeys.IrasId] = irasApplication.IrasId;

        // Redirect to the Questionnaire Resume action to continue the application process
        return RedirectToAction(nameof(QuestionnaireController.Resume), "Questionnaire", new
        {
            categoryId = categoryId,
            projectRecordId = irasApplication.Id
        });
    }

    public IActionResult CreateApplication() => View(nameof(CreateApplication));

    public IActionResult DocumentUpload(string projectRecordId)
    {
        TempData.TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var documents, true);
        ViewBag.ProjectRecordId = projectRecordId;

        return View(documents);
    }

    [HttpPost]
    public IActionResult Upload(IFormFileCollection formFiles, string projectRecordId)
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

        return RedirectToAction(nameof(DocumentUpload), new { projectRecordId });
    }

    [AllowAnonymous]
    public IActionResult ViewportTesting()
    {
        return View();
    }

    [HttpPost]
    [CmsContentAction(nameof(Welcome))]
    public async Task<IActionResult> ApplyFilters(ApplicationsViewModel model)
    {
        var validationResult = await searchValidator.ValidateAsync(model.Search);

        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(nameof(Index), model);
        }

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model.Search));
        return RedirectToAction(nameof(Welcome));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveFilter(string key)
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json) ||
            JsonSerializer.Deserialize<ApplicationSearchModel>(json) is not { } search)
            return RedirectToAction(nameof(Welcome));

        var k = key?.ToLowerInvariant().Replace(" ", "");

        void ClearFromDate() => search.FromDay = search.FromMonth = search.FromYear = null;
        void ClearToDate() => search.ToDay = search.ToMonth = search.ToYear = null;

        var actions = new Dictionary<string, Action>(StringComparer.Ordinal)
        {
            ["datecreated"] = () => { ClearFromDate(); ClearToDate(); },
            ["datecreated-from"] = ClearFromDate,
            ["datecreated-to"] = ClearToDate,
            ["status"] = () => search.Status.Clear()
        };

        if (k is not null && actions.TryGetValue(k, out var act))
            act();

        HttpContext.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(search));
        return await ApplyFilters(new ApplicationsViewModel { Search = search });
    }

    [HttpGet]
    public IActionResult ClearFilters()
    {
        var json = HttpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        if (string.IsNullOrWhiteSpace(json))
            return RedirectToAction(nameof(Welcome));

        if (JsonSerializer.Deserialize<ApplicationSearchModel>(json) is { } search)
        {
            HttpContext.Session.SetString(
                SessionKeys.ProjectRecordSearch,
                JsonSerializer.Serialize(new ApplicationSearchModel { SearchTitleTerm = search.SearchTitleTerm })
            );
        }

        return RedirectToAction(nameof(Welcome));
    }
}