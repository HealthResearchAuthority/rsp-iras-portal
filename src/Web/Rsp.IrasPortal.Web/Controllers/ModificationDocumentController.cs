using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController : Controller
{
    private const string ContainerName = "staging";

    /// <summary>
    /// Returns the view for uploading project documents.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ProjectDocument()
    {
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationUploadDocumentsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
            ? $"Add documents for {specificAreaOfChange}"
            : string.Empty
        };

        // Retrieve the current organisation search term from TempData
        return View(nameof(UploadDocuments), viewModel);
    }

    /// <summary>
    /// Handles the upload of project modification documents to blob storage.
    /// Saves metadata to the database and redirects to the review page.
    /// </summary>
    /// <param name="model">The model containing files to upload and project-related identifiers.</param>
    /// <returns>A redirection to the review view if upload is successful; otherwise, returns the current view with validation errors.</returns>
    [HttpPost]
    public async Task<IActionResult> UploadDocuments(ModificationUploadDocumentsViewModel model)
    {
        // Validate file input
        if (!ModelState.IsValid || model.Files == null || !model.Files.Any())
        {
            ModelState.AddModelError("Files", "Please upload at least one document");
            return View(model);
        }

        // Upload files to blob storage and return metadata (URIs, sizes, etc.)
        var uploadedBlobs = await blobStorageService.UploadFilesAsync(
            model.Files,
            ContainerName,
            model.IrasId.ToString());

        // Retrieve contextual identifiers from TempData and HttpContext
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModificationChangeId);
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Map uploaded blob metadata to document request DTOs
        var uploadedDocuments = new List<ProjectModificationDocumentRequest>();
        foreach (var uploadedBlob in uploadedBlobs)
        {
            var document = new ProjectModificationDocumentRequest
            {
                ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
                ProjectRecordId = projectRecordId,
                ProjectPersonnelId = respondentId,
                FileName = uploadedBlob.FileName,
                DocumentStoragePath = uploadedBlob.BlobUri,
                FileSize = uploadedBlob.FileSize
            };
            uploadedDocuments.Add(document);
        }

        // Save uploaded document metadata to the backend service
        await projectModificationsService.CreateDocumentModification(uploadedDocuments);

        return RedirectToAction(nameof(ReviewDocument));
    }

    /// <summary>
    /// Displays the review page for uploaded project modification documents.
    /// Fetches document metadata from the backend service.
    /// </summary>
    /// <returns>The review view with the list of uploaded documents or an error message if none found.</returns>
    [HttpGet]
    public async Task<IActionResult> ReviewDocument()
    {
        // Fetch contextual data for the view
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationReviewDocumentsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Documents added for {specificAreaOfChange}"
                : string.Empty
        };

        // Create request for fetching documents
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Call the respondent service to retrieve uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            viewModel.UploadedDocuments = response.Content.Select(
                a => new DocumentSummaryItemDto
                {
                    FileName = a.FileName,
                    FileSize = a.FileSize ?? 0,
                    BlobUri = a.DocumentStoragePath
                }).ToList();
        }
        else
        {
            // Handle the case where no documents were returned or service failed
            viewModel.UploadedDocuments = new List<DocumentSummaryItemDto>();
            ModelState.AddModelError(string.Empty, "No documents found or an error occurred while retrieving documents.");
        }

        return View("ModificationReviewDocuments", viewModel);
    }

    [HttpPost]
    public IActionResult AddAnotherDocument()
    {
        return RedirectToAction(nameof(UploadDocuments));
    }

    [HttpGet]
    public async Task<IActionResult> AddDocumentDetailsList()
    {
        // Fetch contextual data for the view
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationReviewDocumentsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Add document details for {specificAreaOfChange}"
                : string.Empty
        };

        // Create request for fetching documents
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Call the respondent service to retrieve uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            viewModel.UploadedDocuments = response.Content.Select(a =>
            {
                var isIncomplete =
                    string.IsNullOrWhiteSpace(a.SponsorDocumentVersion) ||
                    string.IsNullOrWhiteSpace(a.DocumentStoragePath) ||
                    !a.FileSize.HasValue ||
                    !a.SponsorDocumentDate.HasValue ||
                    !a.HasPreviousVersion.HasValue;

                return new DocumentSummaryItemDto
                {
                    FileName = $"Add details for {a.FileName}",
                    FileSize = a.FileSize ?? 0,
                    BlobUri = a.DocumentStoragePath,
                    Status = (isIncomplete ? DocumentDetailStatus.Incomplete : DocumentDetailStatus.Completed).ToString(),
                };
            }).ToList();
        }
        else
        {
            // Handle the case where no documents were returned or service failed
            viewModel.UploadedDocuments = new List<DocumentSummaryItemDto>();
            ModelState.AddModelError(string.Empty, "No documents found or an error occurred while retrieving documents.");
        }

        return View("AddDocumentsDetailsList", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ContinueToDetails()
    {
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Add document details for {specificAreaOfChange}"
                : string.Empty
        };

        ProjectModificationDocumentRequest? doc = null;

        // Check if there's a single "next document" in TempData
        if (TempData.ContainsKey(TempDataKeys.NextDocumentToShow))
        {
            var nextDocJson = TempData[TempDataKeys.NextDocumentToShow] as string;
            doc = JsonSerializer.Deserialize<ProjectModificationDocumentRequest>(nextDocJson!);
        }
        else
        {
            // Otherwise call the service and store remaining docs
            var documentChangeRequest = new ProjectModificationDocumentRequest
            {
                ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModificationChangeId)!,
                ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
                ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
            };

            var response = await respondentService.GetModificationChangesDocuments(
                documentChangeRequest.ProjectModificationChangeId,
                documentChangeRequest.ProjectRecordId,
                documentChangeRequest.ProjectPersonnelId);

            if (response?.StatusCode == HttpStatusCode.OK && response.Content?.Any() == true)
            {
                var documents = response.Content.ToList();

                doc = documents.First();

                var remaining = documents.Skip(1).ToList();
                TempData[TempDataKeys.RemainingDocuments] = JsonSerializer.Serialize(remaining);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "No documents found or an error occurred while retrieving documents.");
            }
        }

        // Populate ViewModel if document found
        if (doc != null)
        {
            viewModel.DocumentId = doc.Id;
            viewModel.FileName = doc.FileName;
            viewModel.FileSize = FormatFileSize(doc.FileSize);
            viewModel.DocumentTypeId = doc.DocumentTypeId;
            viewModel.DocumentStoragePath = doc.DocumentStoragePath;
            viewModel.SponsorDocumentVersion = doc.SponsorDocumentVersion;
            viewModel.HasPreviousVersionApproved = doc.HasPreviousVersion;
            viewModel.SponsorDocumentDateDay = doc.SponsorDocumentDate?.Day;
            viewModel.SponsorDocumentDateMonth = doc.SponsorDocumentDate?.Month;
            viewModel.SponsorDocumentDateYear = doc.SponsorDocumentDate?.Year;
        }

        return View("AddDocumentDetails", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveDocumentDetails(ModificationAddDocumentDetailsViewModel viewModel)
    {
        var validationResult = await documentDetailValidator.ValidateAsync(new ValidationContext<ModificationAddDocumentDetailsViewModel>(viewModel));
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View("AddDocumentDetails", viewModel);
        }

        var documentsRequest = new List<ProjectModificationDocumentRequest>();
        var documentRequest = new ProjectModificationDocumentRequest
        {
            Id = viewModel.DocumentId,
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
            DocumentTypeId = viewModel.DocumentTypeId ?? Guid.Empty,
            FileName = viewModel.FileName,
            DocumentStoragePath = viewModel.DocumentStoragePath,
            //FileSize = viewModel.FileSize != null ? long.Parse(viewModel.FileSize) : null,
            SponsorDocumentVersion = viewModel.SponsorDocumentVersion,
            HasPreviousVersion = viewModel.HasPreviousVersionApproved,
            SponsorDocumentDate = new DateTime(
                viewModel.SponsorDocumentDateYear ?? DateTime.Now.Year,
                viewModel.SponsorDocumentDateMonth ?? 1,
                viewModel.SponsorDocumentDateDay ?? 1)
        };
        documentsRequest.Add(documentRequest);

        // Call the service to save the document details
        var saveResponse = respondentService.SaveModificationDocuments(documentsRequest);

        // Get the next document to show from TempData
        var remainingJson = TempData.Peek(TempDataKeys.RemainingDocuments) as string;
        var remainingDocs = string.IsNullOrWhiteSpace(remainingJson)
            ? new List<ProjectModificationDocumentRequest>()
            : JsonSerializer.Deserialize<List<ProjectModificationDocumentRequest>>(remainingJson)!;

        if (remainingDocs.Any())
        {
            var next = remainingDocs.First();
            TempData[TempDataKeys.RemainingDocuments] = JsonSerializer.Serialize(remainingDocs.Skip(1).ToList());

            // Redirect to ContinueToDetails to handle next doc
            TempData[TempDataKeys.NextDocumentToShow] = JsonSerializer.Serialize(next);
            return RedirectToAction(nameof(ContinueToDetails));
        }

        // All done — go to review
        return RedirectToAction("ReviewDocumentsAnswers");
    }

    private string FormatFileSize(long? bytes)
    {
        if (bytes == null) return "0 B";
        double size = (double)bytes;
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.#} {sizes[order]}";
    }
}