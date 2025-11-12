using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;

/// <summary>
/// Controller responsible for handling project modification documents related actions.
/// </summary>
[Route("modifications/documents/[action]", Name = "pmc:[action]")]
public class DocumentsController
    (
        IProjectModificationsService projectModificationsService,
        IRespondentService respondentService,
        ICmsQuestionsetService cmsQuestionsetService,
        IValidator<QuestionnaireViewModel> validator,
        IBlobStorageService blobStorageService
    ) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator)
{
    private const string StagingContainerName = "staging";
    private const string CleanContainerName = "clean";
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string PostApprovalRoute = "pov:postapproval";

    private const string MissingDateErrorMessage = "Enter a sponsor document date";
    private const string DuplicateDocumentNameErrorMessage = "Document name already exists. Enter a unique document name";

    /// <summary>
    /// Handles GET requests for the ProjectDocument action.
    /// This action prepares the view model for uploading project documents
    /// by reading relevant metadata from TempData (such as the project modification context).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ProjectDocument()
    {
        // Populate a new upload documents view model with common project modification properties
        // (e.g., project record ID, modification ID) pulled from TempData.
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationUploadDocumentsViewModel());

        // Retrieve contextual identifiers from TempData and HttpContext
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;

        // Fetch existing documents for this modification
        var response = await respondentService.GetModificationChangesDocuments(
            viewModel.ModificationId == null ? Guid.Empty : Guid.Parse(viewModel.ModificationId),
            viewModel.ProjectRecordId,
            respondentId);

        // Map and validate existing documents
        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            viewModel.UploadedDocuments = MapDocuments(response.Content);
        }

        // Render the UploadDocuments view, passing in the populated view model.
        return View(nameof(UploadDocuments), viewModel);
    }

    /// <summary>
    /// Displays the review page for uploaded project modification documents.
    /// This action retrieves document metadata from the backend service,
    /// builds a review model, and renders the review view.
    /// </summary>
    /// <returns>
    /// The <see cref="IActionResult"/> for the review page:
    /// - A populated list of uploaded documents if retrieval is successful.
    /// - An empty list with an error message if no documents are found or the service call fails.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ModificationDocumentsAdded()
    {
        // Build a base review model with shared project modification metadata (from TempData, session, etc.)
        // The string parameter is used as a prefix for the review page title.
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationReviewDocumentsViewModel());

        // Construct the request object containing identifiers needed by the service call.
        var request = BuildDocumentRequest();

        // Call the respondent service to fetch metadata for documents
        // that have already been uploaded for this project modification.
        var response = await respondentService.GetModificationChangesDocuments(
            request.ProjectModificationId, request.ProjectRecordId, request.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            // Map the backend service response into DTOs suitable for the view model.
            // Each document entry includes filename, size, and blob URI for download.
            // Documents are ordered alphabetically by filename for consistent display.
            viewModel.UploadedDocuments = [.. response.Content
            .Select(a => new DocumentSummaryItemDto
            {
                DocumentId = a.Id,
                FileName = a.FileName,
                FileSize = a.FileSize ?? 0,
                BlobUri = a.DocumentStoragePath ?? string.Empty,
            })
            .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)];
        }
        else
        {
            // If the service call failed or returned no documents, show an empty list
            // and register a model error so the user is aware of the issue.
            viewModel.UploadedDocuments = [];
            ModelState.AddModelError(
                string.Empty,
                "No documents found or an error occurred while retrieving documents"
            );
        }

        // Render the view with the populated (or empty) view model.
        return View(nameof(ModificationDocumentsAdded), viewModel);
    }

    /// <summary>
    /// Displays the list of uploaded project modification documents
    /// and indicates whether additional details need to be provided for each.
    /// </summary>
    /// <returns>
    /// A view showing the list of uploaded documents, each annotated with its current detail status.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> AddDocumentDetailsList()
    {
        // Populate base model properties
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationReviewDocumentsViewModel());

        // Construct request identifiers
        var documentChangeRequest = BuildDocumentRequest();

        // Retrieve document detail completion statuses
        viewModel.UploadedDocuments = await GetDocumentCompletionStatuses(documentChangeRequest);

        return View(nameof(AddDocumentDetailsList), viewModel);
    }

    /// <summary>
    /// Displays the "Add Document Details" view for a specific uploaded document.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document whose details are being edited or reviewed.</param>
    /// <param name="reviewAnswers">
    /// A flag indicating whether the user is reviewing previously submitted answers
    /// (<c>true</c>) or entering new answers (<c>false</c>).
    /// </param>
    /// <returns>
    /// A view that allows the user to provide or review details for the selected document.
    /// Redirects back to <see cref="AddDocumentDetailsList"/> if document details cannot be retrieved.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ContinueToDetails(Guid documentId, bool reviewAnswers = false)
    {
        // Attempt to fetch the document details from the respondent service.
        var documentDetailsResponse = await respondentService.GetModificationDocumentDetails(documentId);

        // Handle error scenarios: if the service call fails or no content is returned.
        if (documentDetailsResponse?.StatusCode != HttpStatusCode.OK || documentDetailsResponse.Content == null)
        {
            ModelState.AddModelError(string.Empty,
                "Document details not found or an error occurred while retrieving them");
            return RedirectToAction(nameof(AddDocumentDetailsList));
        }

        // Populate the view model with base project and document metadata.
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            DocumentId = documentDetailsResponse.Content.Id,
            FileName = documentDetailsResponse.Content.FileName,
            FileSize = documentDetailsResponse.Content.FileSize ?? 0,
            DocumentStoragePath = documentDetailsResponse.Content.DocumentStoragePath,
            ReviewAnswers = reviewAnswers
        };

        // Fetch the CMS question set that defines what metadata must be collected for this document.
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the questionnaire model containing all questions for the details section.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Retrieve any existing answers the user may have already provided for this document.
        var answersResponse = await respondentService.GetModificationDocumentAnswers(documentId);
        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
            ? answersResponse.Content ?? new List<ProjectModificationDocumentAnswerDto>()
            : new List<ProjectModificationDocumentAnswerDto>();

        // If answers exist, map them onto the corresponding questions in the questionnaire.
        if (answers.Any())
        {
            foreach (var ans in answers)
            {
                // Match the answer to its corresponding question by QuestionId.
                var matchingQuestion = questionnaire.Questions.FirstOrDefault(q => q.QuestionId == ans.QuestionId);
                if (matchingQuestion != null)
                {
                    matchingQuestion.Id = ans.Id;                     // Answer record ID
                    matchingQuestion.AnswerText = ans.AnswerText;     // Free-text answer (if applicable)
                    matchingQuestion.SelectedOption = ans.SelectedOption; // Option-based answer (if applicable)
                }
            }
        }

        // Attach the populated questionnaire to the view model.
        viewModel.Questions = questionnaire.Questions;

        // Render the "Add Document Details" view for the selected document.
        return View("AddDocumentDetails", viewModel);
    }

    /// <summary>
    /// Displays the review page for all uploaded project modification documents.
    /// Aggregates all documents and their responses for display in a single view.
    /// </summary>
    /// <returns>
    /// A view showing the list of documents along with the applicant's answers for review.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> ReviewDocumentDetails()
    {
        // Retrieve all uploaded documents along with their saved responses.
        var viewModels = await GetAllDocumentsWithResponses();

        // Pass the documents and answers to the view for review.
        return View("ReviewDocumentDetails", viewModels);
    }

    /// <summary>
    /// Handles the "Save and Continue" action on the Review Document Details page.
    /// Validates all answers across all documents and redirects accordingly.
    /// </summary>
    /// <returns>
    /// - If validation fails: redisplays the review page with errors.
    /// - If validation passes: redirects to the PostApproval action in ProjectOverview controller.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> ReviewAllDocumentDetails()
    {
        // Fetch all documents along with their existing responses.
        var allDocumentDetails = await GetAllDocumentsWithResponses();

        bool hasFailures = false;
        foreach (var documentDetail in allDocumentDetails)
        {
            var isValid = await this.ValidateQuestionnaire(validator, documentDetail, true);
            if (!isValid)
            {
                hasFailures = true;
            }
        }

        if (hasFailures)
        {
            // Return the view with the invalid models and ModelState errors
            return View("ReviewDocumentDetails", allDocumentDetails);
        }

        // If validation passes, proceed to the sponsor-reference step.
        return RedirectToAction(
            nameof(SponsorReferenceController.SponsorReference),
            "SponsorReference",
            new { projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty });
    }

    /// <summary>
    /// Handles the "Add Another Document" action from the review or upload pages.
    /// Redirects the user to the document upload page.
    /// </summary>
    /// <returns>A redirection to the UploadDocuments action.</returns>
    [HttpPost]
    public IActionResult AddAnotherDocument()
    {
        return RedirectToAction(nameof(UploadDocuments));
    }

    /// <summary>
    /// Saves the answers provided for a specific document’s questions.
    /// Updates the model with user responses and validates the questionnaire.
    /// </summary>
    /// <param name="viewModel">The document details and responses submitted by the user.</param>
    /// <returns>
    /// - If validation fails: redisplays the AddDocumentDetails view with validation errors.
    /// - If validation succeeds: saves answers and redirects to review or list page based on ReviewAnswers flag.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> SaveDocumentDetails(ModificationAddDocumentDetailsViewModel viewModel, bool saveForLater = false)
    {
        // Step 1: Retrieve and rebuild the questionnaire structure from CMS
        var questionnaire = await BuildUpdatedQuestionnaire(viewModel);

        // Replace the original question list with updated answers from the user
        viewModel.Questions = questionnaire.Questions;

        // Find the "Document Name" question for later duplicate name validation
        var documentNameQuestion = questionnaire.Questions
            .Select((q, index) => new { Question = q, Index = index })
            .FirstOrDefault(x => x.Question.QuestionId.Equals(QuestionIds.DocumentName, StringComparison.OrdinalIgnoreCase));

        // Step 2: Validate all basic questionnaire rules (e.g., required fields)
        var isValid = await this.ValidateQuestionnaire(validator, viewModel);

        // Step 3: Validate missing date fields — only if user is not saving for later
        if (!saveForLater)
        {
            var dateValidationPassed = ValidateRequiredDates(viewModel);
            isValid = isValid && dateValidationPassed;
        }

        // Step 4: Validate that document name doesn’t already exist
        var duplicateValidationPassed = await ValidateDuplicateDocumentNames(viewModel, documentNameQuestion);
        isValid = isValid && duplicateValidationPassed;

        // Pass validation result to the view so that GOV.UK error summaries can display correctly
        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        // Step 5: If validation fails, redisplay form with errors
        if (!isValid)
            return View("AddDocumentDetails", viewModel);

        // Step 6: Save all valid answers to backend
        await SaveModificationDocumentAnswers(viewModel);

        // Step 7: Redirect user depending on their action
        return saveForLater
            ? RedirectToSaveForLater()    // “Save and come back later”
            : RedirectAfterSubmit(viewModel); // Continue flow or review answers
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDeleteDocument(Guid id, string backRoute)
    {
        // Attempt to fetch the document details from the respondent service.
        var documentDetailsResponse = await respondentService.GetModificationDocumentDetails(id);

        // Construct the request object containing identifiers needed by the service call
        var request = BuildDocumentRequest();
        request.Id = id;
        request.FileName = documentDetailsResponse?.Content?.FileName;
        request.FileSize = documentDetailsResponse?.Content?.FileSize ?? 0;
        request.DocumentStoragePath = documentDetailsResponse?.Content?.DocumentStoragePath;

        var viewModel = new ModificationDeleteDocumentViewModel
        {
            Documents = [request]
        };

        viewModel.BackRoute = backRoute;
        return View("DeleteDocuments", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmDeleteDocuments(string? backRoute)
    {
        // Construct the request object containing identifiers needed by the service call
        var request = BuildDocumentRequest();

        // Call the respondent service to fetch metadata for documents
        var response = await respondentService.GetModificationChangesDocuments(
            request.ProjectModificationId, request.ProjectRecordId, request.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            // Map the backend service response into DTOs suitable for the view model
            var viewModel = new ModificationDeleteDocumentViewModel
            {
                Documents = [.. response.Content.Select(doc => new ProjectModificationDocumentRequest
                {
                    Id = doc.Id,
                    ProjectModificationId = request.ProjectModificationId,
                    ProjectRecordId = request.ProjectRecordId,
                    ProjectPersonnelId = request.ProjectPersonnelId,
                    FileName = doc.FileName,
                    DocumentStoragePath = doc.DocumentStoragePath,
                    FileSize = doc.FileSize
                })
                .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)],
                BackRoute = backRoute
            };
            return View("DeleteDocuments", viewModel);
        }

        return RedirectToAction(nameof(ProjectDocument));
    }

    [HttpPost("deletedocument")]
    public async Task<IActionResult> DeleteDocuments(ModificationDeleteDocumentViewModel model)
    {
        if (model?.Documents == null || !model.Documents.Any())
        {
            // No documents to delete
            return RedirectToAction(nameof(AddDocumentDetailsList));
        }
        var request = BuildDocumentRequest();

        var multipleDelete = model.Documents.Count > 1;

        // Build the request list from the view model
        var deleteDocumentRequest = model.Documents
            .Select(d => new ProjectModificationDocumentRequest
            {
                Id = d.Id,
                ProjectModificationId = request.ProjectModificationId,
                ProjectRecordId = request.ProjectRecordId,
                ProjectPersonnelId = request.ProjectPersonnelId,
                FileName = d.FileName,
                DocumentStoragePath = d.DocumentStoragePath,
                FileSize = d.FileSize,
                Status = d.Status
            })
            .ToList();

        // Call the service to delete the documents
        var deleteResponse = await projectModificationsService.DeleteDocumentModification(deleteDocumentRequest);

        // Delete from the appropriate blob container based on document status
        foreach (var doc in deleteDocumentRequest)
        {
            if (string.IsNullOrEmpty(doc.DocumentStoragePath))
                continue;

            // Choose container depending on whether the document upload succeeded
            var targetContainer = doc.Status.Equals(DocumentStatus.Success, StringComparison.OrdinalIgnoreCase)
                ? CleanContainerName
                : StagingContainerName;

            await blobStorageService.DeleteFileAsync(
                containerName: targetContainer,
                blobPath: doc.DocumentStoragePath
            );
        }

        // Handle single vs multiple delete redirection
        if (!multipleDelete)
        {
            // Call the respondent service to fetch metadata for documents
            var response = await respondentService.GetModificationChangesDocuments(
                request.ProjectModificationId, request.ProjectRecordId, request.ProjectPersonnelId);

            // Check if there are any remaining documents in the response
            if (response?.Content == null || !response.Content.Any())
            {
                // No more documents → go to ProjectDocument view
                return RedirectToAction(nameof(ProjectDocument));
            }

            // Documents still exist → go back to AddDocumentDetailsList
            return RedirectToAction(nameof(AddDocumentDetailsList));
        }
        else
        {
            // After multiple deletes go to ProjectDocument view
            return RedirectToAction(nameof(ProjectDocument));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string path, string fileName)
    {
        var serviceResponse = await blobStorageService
            .DownloadFileToHttpResponseAsync(StagingContainerName, path, fileName);

        return serviceResponse?.Content!;
    }

    /// <summary>
    /// Handles the upload of project modification documents to blob storage.
    /// Persists metadata to the backend service and redirects to the review page.
    /// </summary>
    /// <param name="model">Contains files to upload and related project identifiers.</param>
    /// <returns>
    /// - If files are successfully uploaded: redirects to the review page.
    /// - If documents already exist: redirects to the review page.
    /// - If no files uploaded or service errors occur: returns the current view with validation errors.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> UploadDocuments(ModificationUploadDocumentsViewModel model)
    {
        // If the posted model is null (due to exceeding max request size)
        if (model?.Files == null)
            return View("FileTooLarge");

        // Retrieve contextual identifiers from TempData and HttpContext
        var projectModificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId);
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
        var irasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;

        // Fetch existing documents for this modification
        var response = await respondentService.GetModificationChangesDocuments(
            projectModificationId == null ? Guid.Empty : (Guid)projectModificationId!,
            projectRecordId,
            respondentId);

        // Map and validate existing documents
        var atleastOneInvalidFile = false;
        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            model.UploadedDocuments = MapDocuments(response.Content);
            atleastOneInvalidFile = ValidateExistingDocuments(model);
        }

        // If no files were selected but some invalid exist
        if (model.Files == null || model.Files.Count == 0)
            return HandleNoNewFiles(response, atleastOneInvalidFile, model);

        // Validate new uploaded files
        var validationResult = ValidateUploadedFiles(model.Files, response?.Content?.ToList() ?? []);
        atleastOneInvalidFile = validationResult.HasErrors;
        if (atleastOneInvalidFile)
            return View(model);

        // Upload valid files to blob storage
        var uploadedDocuments = await UploadValidFilesAsync(validationResult.ValidFiles, irasId, projectModificationId, projectRecordId, respondentId);

        // Append and sort the newly uploaded documents
        model.UploadedDocuments = AppendAndSortDocuments(model.UploadedDocuments, uploadedDocuments);

        // Stay on the same view to show all documents
        return View(model);
    }

    private static List<DocumentSummaryItemDto> MapDocuments(IEnumerable<ProjectModificationDocumentRequest> content)
    {
        // Map the backend service response into DTOs suitable for the view model.
        // Each document entry includes filename, size, and blob URI for download.
        return [.. content
            .Select(a => new DocumentSummaryItemDto
            {
                DocumentId = a.Id,
                FileName = a.FileName,
                FileSize = a.FileSize ?? 0,
                BlobUri = a.DocumentStoragePath ?? string.Empty,
                Status = a.Status ?? string.Empty
            })
            .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)];
    }

    private bool ValidateExistingDocuments(ModificationUploadDocumentsViewModel model)
    {
        // Check if any uploaded document has a failure status (case-insensitive)
        var invalid = model.UploadedDocuments
            .Any(d => d.Status.Equals(DocumentStatus.Failed, StringComparison.OrdinalIgnoreCase));

        if (invalid)
            ModelState.AddModelError("Files", "Failed documents should be deleted before continuing");

        return invalid;
    }

    private (List<IFormFile> ValidFiles, bool HasErrors) ValidateUploadedFiles(
        IEnumerable<IFormFile> files,
        List<ProjectModificationDocumentRequest> existingDocs)
    {
        var validFiles = new List<IFormFile>();
        const long maxFileSize = 100 * 1024 * 1024; // 100 MB in bytes
        long totalFileSize = 0;
        bool hasErrors = false;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(file.FileName);

            // 1. Extension check
            if (!FileConstants.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                hasErrors = true;
                ModelState.AddModelError("Files", $"{fileName} must be a permitted file type");
                continue;
            }

            // 2. Duplicate file name check
            if (existingDocs.Any(d => d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                hasErrors = true;
                ModelState.AddModelError("Files", $"{fileName} has already been uploaded");
                continue;
            }

            // 3. File size check
            if (file.Length > maxFileSize)
            {
                hasErrors = true;
                ModelState.AddModelError("Files", $"{fileName} must be smaller than 100 MB");
                continue;
            }

            totalFileSize += file.Length;
            validFiles.Add(file);
        }

        // 4. Combined file size check
        if (totalFileSize > maxFileSize)
        {
            hasErrors = true;
            ModelState.AddModelError("Files", "The combined size of all files must not exceed 100 MB");
        }

        return (validFiles, hasErrors);
    }

    private async Task<List<ProjectModificationDocumentRequest>> UploadValidFilesAsync(
        List<IFormFile> validFiles,
        string irasId,
        object? projectModificationId,
        string projectRecordId,
        string respondentId)
    {
        // Upload only valid files to blob storage
        var uploadedBlobs = await blobStorageService.UploadFilesAsync(validFiles, StagingContainerName, irasId);

        // Map uploaded blob metadata to DTOs for backend service
        var uploadedDocuments = uploadedBlobs.ConvertAll(blob => new ProjectModificationDocumentRequest
        {
            ProjectModificationId = projectModificationId == null ? Guid.Empty : (Guid)projectModificationId!,
            ProjectRecordId = projectRecordId,
            ProjectPersonnelId = respondentId,
            FileName = blob.FileName,
            DocumentStoragePath = blob.BlobUri,
            FileSize = blob.FileSize,
            Status = DocumentStatus.Uploaded
        });

        await projectModificationsService.CreateDocumentModification(uploadedDocuments);
        return uploadedDocuments;
    }

    private static List<DocumentSummaryItemDto> AppendAndSortDocuments(
        List<DocumentSummaryItemDto>? existing,
        List<ProjectModificationDocumentRequest> uploaded)
    {
        // Append newly uploaded documents to existing ones, ensuring consistent alphabetical order
        return [.. (existing ?? [])
            .Concat(uploaded.Select(a => new DocumentSummaryItemDto
            {
                DocumentId = a.Id,
                FileName = a.FileName,
                FileSize = a.FileSize ?? 0,
                BlobUri = a.DocumentStoragePath,
                Status = a.Status ?? string.Empty
            }))
            .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)];
    }

    private IActionResult HandleNoNewFiles(
        ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>? response,
        bool atleastOneInvalidFile,
        ModificationUploadDocumentsViewModel model)
    {
        if (atleastOneInvalidFile)
            return View(model);

        if (response?.StatusCode != HttpStatusCode.OK)
            return this.ServiceError(response!);

        if (response.Content?.Any() == true)
            return RedirectToAction(nameof(ModificationDocumentsAdded));

        ModelState.AddModelError("Files", "Please upload at least one document");
        return View(model);
    }

    /// <summary>
    /// Saves answers submitted for a document’s questions to the backend service.
    /// Handles mapping between view model, CMS questions, and API DTOs.
    /// </summary>
    /// <param name="viewModel">The view model containing user responses.</param>
    private async Task SaveModificationDocumentAnswers(ModificationAddDocumentDetailsViewModel viewModel)
    {
        var request = new List<ProjectModificationDocumentAnswerDto>();

        foreach (var question in viewModel.Questions)
        {
            // Determine if question expects a single or multiple responses
            var optionType = question.DataType switch
            {
                "Boolean" or "Radio button" or "Look-up list" or "Dropdown" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            // Map question responses into DTO
            request.Add(new ProjectModificationDocumentAnswerDto
            {
                Id = question.Id,
                ModificationDocumentId = viewModel.DocumentId,
                QuestionId = question.QuestionId,
                VersionId = question.VersionId ?? string.Empty,
                AnswerText = question.AnswerText,
                CategoryId = question.Category,
                SectionId = question.SectionId,
                SelectedOption = question.SelectedOption,
                OptionType = optionType,
                Answers = question.Answers
                                .Where(a => a.IsSelected)
                                .Select(ans => ans.AnswerId)
                                .ToList()
            });
        }

        // Save responses only if there is at least one answer
        if (request.Count > 0)
        {
            await respondentService.SaveModificationDocumentAnswers(request);
        }
    }

    /// <summary>
    /// Retrieves all uploaded modification documents with their respective answers.
    /// Constructs a view model for displaying the documents and questions.
    /// </summary>
    /// <returns>A list of <see cref="ModificationAddDocumentDetailsViewModel"/> with answers populated.</returns>
    private async Task<IList<ModificationAddDocumentDetailsViewModel>> GetAllDocumentsWithResponses()
    {
        var documentChangeRequest = BuildDocumentRequest();

        // Fetch uploaded documents for the modification
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        // Retrieve the CMS question set for the document details section
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        var cmsQuestions = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        var viewModels = new List<ModificationAddDocumentDetailsViewModel>();
        var questionIndex = 0;

        foreach (var doc in response!.Content.OrderBy(d => d.FileName, StringComparer.OrdinalIgnoreCase))
        {
            // Fetch existing answers for this document
            var answersResponse = await respondentService.GetModificationDocumentAnswers(doc.Id);
            var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                ? answersResponse.Content ?? new List<ProjectModificationDocumentAnswerDto>()
                : new List<ProjectModificationDocumentAnswerDto>();

            // Map document and questions into a view model
            var vm = new ModificationAddDocumentDetailsViewModel
            {
                DocumentId = doc.Id,
                FileName = doc.FileName,
                DocumentStoragePath = doc.DocumentStoragePath,
                Status = doc.Status,
                ReviewAnswers = true,
                Questions = cmsQuestions.Questions.Select(cmsQ =>
                {
                    var matchingAnswer = answers.FirstOrDefault(a => a.QuestionId == cmsQ.QuestionId);

                    return new QuestionViewModel
                    {
                        Id = matchingAnswer?.Id,
                        Index = questionIndex++,
                        QuestionId = cmsQ.QuestionId,
                        VersionId = cmsQ.VersionId,
                        Category = cmsQ.Category,
                        SectionId = cmsQ.SectionId,
                        Section = cmsQ.Section,
                        Sequence = cmsQ.Sequence,
                        Heading = cmsQ.Heading,
                        QuestionText = cmsQ.QuestionText,
                        QuestionType = cmsQ.QuestionType,
                        DataType = cmsQ.DataType,
                        IsMandatory = cmsQ.IsMandatory,
                        IsOptional = cmsQ.IsOptional,
                        AnswerText = matchingAnswer?.AnswerText,
                        SelectedOption = matchingAnswer?.SelectedOption,
                        Answers = cmsQ?.Answers?
                        .Select(ans => new AnswerViewModel
                        {
                            AnswerId = ans.AnswerId,
                            AnswerText = ans.AnswerText,
                            // Set true if CMS already marked it OR if it exists in answersResponse
                            IsSelected = ans.IsSelected || answers.Any(a => a.SelectedOption == ans.AnswerId)
                        })
                        .ToList() ?? new List<AnswerViewModel>(),
                        Rules = cmsQ?.Rules ?? new List<RuleDto>(),
                        ShortQuestionText = cmsQ?.ShortQuestionText ?? string.Empty,
                        IsModificationQuestion = true,
                        GuidanceComponents = cmsQ?.GuidanceComponents ?? new List<ComponentContent>()
                    };
                }).ToList()
            };

            viewModels.Add(vm);
        }

        return viewModels;
    }

    /// <summary>
    /// Builds the questionnaire structure from CMS and maps user responses onto it.
    /// Ensures that form inputs (AnswerText, Dates, etc.) are bound to correct questions.
    /// </summary>
    private async Task<QuestionnaireViewModel> BuildUpdatedQuestionnaire(ModificationAddDocumentDetailsViewModel viewModel)
    {
        // Get the CMS-driven question set for "Document Details"
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(DocumentDetailsSection);

        // Build questionnaire view model from CMS content
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Map submitted answers to each question
        foreach (var question in questionnaire.Questions)
        {
            var response = viewModel.Questions.Find(q => q.Index == question.Index);

            // Copy selected answer values from the user submission
            question.SelectedOption = response?.SelectedOption;

            // For non-dropdowns, assign user’s text or checkbox answers
            if (question.DataType != "Dropdown")
                question.Answers = response?.Answers ?? [];

            // Carry over IDs and free-text responses
            question.Id = response?.Id;
            question.AnswerText = response?.AnswerText;

            // Handle date fields (day, month, year)
            question.Day = response?.Day;
            question.Month = response?.Month;
            question.Year = response?.Year;
        }

        return questionnaire;
    }

    /// <summary>
    /// Validates that date fields are entered for questions requiring dates,
    /// based on selected document type.
    /// </summary>
    private bool ValidateRequiredDates(ModificationAddDocumentDetailsViewModel viewModel)
    {
        // Identify which document type the user selected
        var selectedDocumentTypeOption = viewModel.Questions
            .FirstOrDefault(q => q.QuestionId == QuestionIds.SelectedDocumentType)?.SelectedOption;

        // Filter all questions that use a date input type
        var dateQuestions = viewModel.Questions.Where(q => q.DataType?.ToLower() == "date");
        var isValid = true;

        foreach (var question in dateQuestions)
        {
            // Find rules that define when this date question should be required
            var optionsWithDate = question.Rules?
                .FirstOrDefault()?.Conditions?
                .FirstOrDefault(c => c.Operator == "IN")?.ParentOptions;

            // If the document type requires a date, and it's missing, mark invalid
            if (ShouldRequireDate(selectedDocumentTypeOption, optionsWithDate) && IsDateMissing(question))
            {
                ModelState.AddModelError($"Questions[{question.Index}].AnswerText", MissingDateErrorMessage);
                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>Checks if the selected document type requires a date.</summary>
    private static bool ShouldRequireDate(string? selectedOption, IEnumerable<string>? optionsWithDate) =>
        selectedOption is not null && optionsWithDate?.Contains(selectedOption) == true;

    /// <summary>Checks if all parts of a date (day/month/year) are missing.</summary>
    private static bool IsDateMissing(QuestionViewModel q) =>
        string.IsNullOrWhiteSpace(q.Day) && string.IsNullOrWhiteSpace(q.Month) && string.IsNullOrWhiteSpace(q.Year);

    /// <summary>
    /// Checks whether the document name entered already exists among uploaded documents.
    /// Adds ModelState error if a duplicate is found.
    /// </summary>
    private async Task<bool> ValidateDuplicateDocumentNames(
            ModificationAddDocumentDetailsViewModel viewModel,
            dynamic? documentNameQuestion)
    {
        var request = BuildDocumentRequest();

        // Fetch all existing uploaded documents for this modification
        var documentsResponse = await respondentService.GetModificationChangesDocuments(
            request.ProjectModificationId,
            request.ProjectRecordId,
            request.ProjectPersonnelId);

        // Skip validation if service call fails or no documents exist
        if (documentsResponse?.StatusCode != HttpStatusCode.OK || documentsResponse.Content == null)
            return true;

        var isValid = true;

        // Extract all document IDs except the current one, ordered alphabetically by FileName
        var documentIds = documentsResponse.Content
            .Where(d => d.Id != viewModel.DocumentId)
            .OrderBy(d => d.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(d => d.Id);

        foreach (var documentId in documentIds)
        {
            // Fetch answers for each document
            var answersResponse = await respondentService.GetModificationDocumentAnswers(documentId);
            var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                ? answersResponse.Content ?? []
                : [];

            // Get the existing document name (if any)
            var existingDocName = answers
                .FirstOrDefault(a => a.QuestionId == QuestionIds.DocumentName)?
                .AnswerText?
                .Trim();

            // Compare document names to detect duplicates
            if (!string.IsNullOrWhiteSpace(existingDocName) &&
                string.Equals(
                    existingDocName,
                    documentNameQuestion?.Question?.AnswerText?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    $"Questions[{documentNameQuestion?.Index}].AnswerText",
                    DuplicateDocumentNameErrorMessage);

                isValid = false;
            }
        }

        return isValid;
    }

    /// <summary>
    /// Redirects to the “save for later” route.
    /// </summary>
    private RedirectToRouteResult RedirectToSaveForLater()
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
    }

    /// <summary>
    /// Redirects user to appropriate next page based on review mode.
    /// </summary>
    private RedirectToActionResult RedirectAfterSubmit(ModificationAddDocumentDetailsViewModel viewModel)
    {
        return viewModel.ReviewAnswers
            ? RedirectToAction(nameof(ReviewDocumentDetails))
            : RedirectToAction(nameof(AddDocumentDetailsList));
    }
}