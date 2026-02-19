using System.Net;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Rsp.Gds.Component.Models;
using Rsp.IrasPortal.Web.Attributes;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses.CmsContent;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.AccessControl;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace Rsp.Portal.Web.Features.Modifications.Documents.Controllers;

/// <summary>
/// Controller responsible for handling project modification documents related actions.
/// </summary>
[Authorize(Policy = Workspaces.MyResearch)]
[Route("modifications/documents/[action]", Name = "pmc:[action]")]
public class DocumentsController
    (
        IProjectModificationsService projectModificationsService,
        IRespondentService respondentService,
        ICmsQuestionsetService cmsQuestionsetService,
        IValidator<QuestionnaireViewModel> validator,
        IBlobStorageService blobStorageService,
        IAzureClientFactory<BlobServiceClient> blobClientFactory,
        IFeatureManager featureManager
    ) : ModificationsControllerBase(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
{
    private const string StagingContainerName = "staging";
    private const string CleanContainerName = "clean";
    private const string DocumentDetailsSection = "pdm-document-metadata";
    private const string PostApprovalRoute = "pov:postapproval";
    private const string ReviewAllChangesRoute = "pmc:reviewallchanges";

    private const string MissingDateErrorMessage = "Enter a sponsor document date";
    private const string DuplicateDocumentNameErrorMessage = "Document name already exists. Enter a unique document name";

    private readonly IAzureClientFactory<BlobServiceClient> _blobClientFactory = blobClientFactory;

    /// <summary>
    /// Handles GET requests for the ProjectDocument action.
    /// This action prepares the view model for uploading project documents
    /// by reading relevant metadata from TempData (such as the project modification context).
    /// </summary>
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
    [HttpGet]
    public async Task<IActionResult> ProjectDocument()
    {
        // Populate a new upload documents view model with common project modification properties
        // (e.g., project record ID, modification ID) pulled from TempData.
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationUploadDocumentsViewModel());

        // Retrieve contextual identifiers from TempData and HttpContext
        var respondentId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;

        // Fetch existing documents for this modification
        var response = await respondentService.GetModificationChangesDocuments(
            viewModel.ModificationId == null ? Guid.Empty : Guid.Parse(viewModel.ModificationId),
            viewModel.ProjectRecordId);

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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
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
            request.ProjectModificationId, request.ProjectRecordId);

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
                IsMalwareScanSuccessful = a.IsMalwareScanSuccessful
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
        return View(viewModel);
    }

    /// <summary>
    /// Displays the list of uploaded project modification documents
    /// and indicates whether additional details need to be provided for each.
    /// </summary>
    /// <returns>
    /// A view showing the list of uploaded documents, each annotated with its current detail status.
    /// </returns>
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
    [HttpGet]
    public async Task<IActionResult> ContinueToDetails(Guid documentId, bool reviewAnswers = false, bool reviewAllChanges = false)
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
            ModificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is Guid modificationId
               ? modificationId
               : Guid.NewGuid(),
            FileName = documentDetailsResponse.Content.FileName,
            FileSize = documentDetailsResponse.Content.FileSize ?? 0,
            DocumentStoragePath = documentDetailsResponse.Content.DocumentStoragePath,
            ReviewAnswers = reviewAnswers,
            ReviewAllChanges = reviewAllChanges,
            IsMalwareScanSuccessful = documentDetailsResponse.Content.IsMalwareScanSuccessful,
            DocumentType = documentDetailsResponse.Content.DocumentType,
            LinkedDocumentId = documentDetailsResponse.Content.LinkedDocumentId,
            ReplacesDocumentId = documentDetailsResponse.Content.ReplacesDocumentId
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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Review)]
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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Review)]
    [HttpPost]
    public async Task<IActionResult> ReviewAllDocumentDetails()
    {
        // Fetch all documents along with their existing responses.
        var allDocumentDetails = await GetAllDocumentsWithResponses();

        bool hasFailures = false;
        for (int docIndex = 0; docIndex < allDocumentDetails.Count; docIndex++)
        {
            ModificationAddDocumentDetailsViewModel? documentDetail = allDocumentDetails[docIndex];
            var isValid = await this.ValidateQuestionnaire(validator, documentDetail, true);
            if (documentDetail.ShowSupersedeDocumentSection)
            {
                if (documentDetail.ReplacesDocumentId is null
                    || string.IsNullOrEmpty(documentDetail.DocumentType)
                    || string.IsNullOrEmpty(documentDetail.LinkedDocumentFileName))
                {
                    isValid = false;
                    if (documentDetail.ReplacesDocumentId is null)
                    {
                        ModelState.AddModelError(
                            $"Questions[{docIndex}].DocumentInService",
                            "Enter document in service");
                    }
                    if (string.IsNullOrEmpty(documentDetail.DocumentType))
                    {
                        ModelState.AddModelError(
                            $"Questions[{docIndex}].CleanOrTracked",
                            "Enter clean or tracked");
                    }
                    if (string.IsNullOrEmpty(documentDetail.LinkedDocumentFileName))
                    {
                        ModelState.AddModelError(
                            $"Questions[{docIndex}].LinkedDocumentFileName",
                            "Enter linked document");
                    }
                }
            }

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

        // get data from session for Revise and authorise
        var modificationModel = TempData.PopulateBaseProjectModificationProperties(new BaseProjectModificationViewModel());
        if (modificationModel.Status is ModificationStatus.ReviseAndAuthorise)
        {
            return RedirectToRoute("pmc:ModificationDetails", new
            {
                modificationModel.ProjectRecordId,
                modificationModel.IrasId,
                modificationModel.ShortTitle,
                projectModificationId = modificationModel.ModificationId,
                modificationModel.SponsorOrganisationUserId,
                modificationModel.RtsId
            });
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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
    [HttpPost]
    public IActionResult AddAnotherDocument()
    {
        return RedirectToAction(nameof(UploadDocuments));
    }

    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Delete)]
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
        request.ReplacesDocumentId = documentDetailsResponse?.Content?.ReplacesDocumentId;
        request.ReplacedByDocumentId = documentDetailsResponse?.Content?.ReplacedByDocumentId;
        request.LinkedDocumentId = documentDetailsResponse?.Content?.LinkedDocumentId;

        var viewModel = new ModificationDeleteDocumentViewModel
        {
            Documents = [request]
        };

        viewModel.BackRoute = backRoute;
        return View("DeleteDocuments", viewModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Delete)]
    [HttpGet]
    public async Task<IActionResult> ConfirmDeleteDocuments(string? backRoute)
    {
        // Construct the request object containing identifiers needed by the service call
        var request = BuildDocumentRequest();

        // Call the respondent service to fetch metadata for documents
        var response = await respondentService.GetModificationChangesDocuments(
            request.ProjectModificationId, request.ProjectRecordId);

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
                    UserId = request.UserId,
                    FileName = doc.FileName,
                    DocumentStoragePath = doc.DocumentStoragePath,
                    FileSize = doc.FileSize,
                    ReplacesDocumentId = doc.ReplacesDocumentId,
                    ReplacedByDocumentId = doc.ReplacedByDocumentId,
                    LinkedDocumentId = doc.LinkedDocumentId
                })
                .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)],
                BackRoute = backRoute
            };
            return View("DeleteDocuments", viewModel);
        }

        return RedirectToAction(nameof(ProjectDocument));
    }

    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Delete)]
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
                UserId = request.UserId,
                FileName = d.FileName,
                DocumentStoragePath = d.DocumentStoragePath,
                FileSize = d.FileSize,
                Status = d.Status,
                IsMalwareScanSuccessful = d.IsMalwareScanSuccessful,
                ReplacedByDocumentId = d.ReplacedByDocumentId,
                ReplacesDocumentId = d.ReplacesDocumentId,
                LinkedDocumentId = d.LinkedDocumentId
            }).ToList();

        var updateDocumentRequest = new List<ProjectModificationDocumentRequest>();
        var deleteDocumentAnswersRequest = new List<ProjectModificationDocumentRequest>();
        foreach (var updateDocument in deleteDocumentRequest)
        {
            if (updateDocument.ReplacesDocumentId.HasValue && updateDocument.ReplacesDocumentId != Guid.Empty)
            {
                var updateDocumentResponse =
                await respondentService.GetModificationDocumentDetails((Guid)updateDocument.ReplacesDocumentId);

                updateDocumentRequest.Add(new ProjectModificationDocumentRequest
                {
                    Id = updateDocumentResponse.Content?.Id ?? Guid.Empty,
                    ProjectModificationId = updateDocumentResponse.Content?.ProjectModificationId ?? default,
                    ProjectRecordId = updateDocumentResponse.Content?.ProjectRecordId,
                    UserId = updateDocumentResponse.Content?.UserId,
                    FileName = updateDocumentResponse.Content?.FileName ?? string.Empty,
                    DocumentStoragePath = updateDocumentResponse.Content?.DocumentStoragePath,
                    FileSize = updateDocumentResponse.Content?.FileSize ?? 0,
                    Status = updateDocumentResponse.Content?.Status,
                    IsMalwareScanSuccessful = updateDocumentResponse.Content?.IsMalwareScanSuccessful,
                    DocumentType = updateDocumentResponse.Content?.DocumentType,
                    ReplacedByDocumentId = null, // Clear the ReplacedByDocumentId to unlink it from the deleted document
                    ReplacesDocumentId = updateDocumentResponse.Content?.ReplacesDocumentId,
                    LinkedDocumentId = updateDocumentResponse.Content?.LinkedDocumentId
                });
            }

            if (updateDocument.ReplacedByDocumentId.HasValue && updateDocument.ReplacedByDocumentId != Guid.Empty)
            {
                var updateDocumentResponse =
                await respondentService.GetModificationDocumentDetails((Guid)updateDocument.ReplacedByDocumentId);
                updateDocumentRequest.Add(new ProjectModificationDocumentRequest
                {
                    Id = updateDocumentResponse.Content?.Id ?? Guid.Empty,
                    ProjectModificationId = updateDocumentResponse.Content?.ProjectModificationId ?? default,
                    ProjectRecordId = updateDocumentResponse.Content?.ProjectRecordId,
                    UserId = updateDocumentResponse.Content?.UserId,
                    FileName = updateDocumentResponse.Content?.FileName ?? string.Empty,
                    DocumentStoragePath = updateDocumentResponse.Content?.DocumentStoragePath,
                    FileSize = updateDocumentResponse.Content?.FileSize ?? 0,
                    Status = updateDocumentResponse.Content?.Status,
                    IsMalwareScanSuccessful = updateDocumentResponse.Content?.IsMalwareScanSuccessful,
                    DocumentType = updateDocumentResponse.Content?.DocumentType,
                    ReplacesDocumentId = null, // Clear the ReplacesDocumentId to unlink it from the deleted document
                    ReplacedByDocumentId = updateDocumentResponse.Content?.ReplacedByDocumentId,
                    LinkedDocumentId = updateDocumentResponse.Content?.LinkedDocumentId
                });
            }

            if (updateDocument.LinkedDocumentId.HasValue && updateDocument.LinkedDocumentId != Guid.Empty)
            {
                var updateDocumentResponse =
                await respondentService.GetModificationDocumentDetails((Guid)updateDocument.LinkedDocumentId);
                var documentType =
                string.Equals(updateDocumentResponse.Content?.DocumentType,
                    SupersedeDocumentsType.Clean,
                    StringComparison.OrdinalIgnoreCase)
                    ? updateDocumentResponse.Content?.DocumentType
                    : null;

                var linkedDocument = new ProjectModificationDocumentRequest
                {
                    Id = updateDocumentResponse.Content?.Id ?? Guid.Empty,
                    ProjectModificationId = updateDocumentResponse.Content?.ProjectModificationId ?? default,
                    ProjectRecordId = updateDocumentResponse.Content?.ProjectRecordId,
                    UserId = updateDocumentResponse.Content?.UserId,
                    FileName = updateDocumentResponse.Content?.FileName ?? string.Empty,
                    DocumentStoragePath = updateDocumentResponse.Content?.DocumentStoragePath,
                    FileSize = updateDocumentResponse.Content?.FileSize ?? 0,
                    Status = updateDocumentResponse.Content?.Status,
                    IsMalwareScanSuccessful = updateDocumentResponse.Content?.IsMalwareScanSuccessful,
                    DocumentType = documentType,
                    ReplacesDocumentId = updateDocumentResponse.Content?.ReplacesDocumentId,
                    ReplacedByDocumentId = updateDocumentResponse.Content?.ReplacedByDocumentId,
                    LinkedDocumentId = null // Clear the LinkedDocumentId to unlink it from the deleted document
                };
                updateDocumentRequest.Add(linkedDocument);

                if (linkedDocument.DocumentType != SupersedeDocumentsType.Clean)
                {
                    deleteDocumentAnswersRequest.Add(linkedDocument);
                }
            }
        }

        // Call the service to delete the documents
        var deleteResponse = await projectModificationsService.DeleteDocumentModification(deleteDocumentRequest);

        // Delete linked document answers
        if (deleteDocumentAnswersRequest.Count > 0)
        {
            await projectModificationsService.DeleteDocumentAnswersModification(deleteDocumentAnswersRequest);
        }

        if (!multipleDelete && updateDocumentRequest.Count > 0)
        {
            await respondentService.SaveModificationDocuments(updateDocumentRequest);
        }

        // Delete from the appropriate blob container based on document status
        foreach (var doc in deleteDocumentRequest)
        {
            if (string.IsNullOrEmpty(doc.DocumentStoragePath))
                continue;

            // Determine whether this document should use the clean container
            bool useClean = doc.IsMalwareScanSuccessful == true;

            // Choose blob client and container based on malware scan result
            var targetBlobClient = GetBlobClient(useClean);
            var targetContainer = useClean ? CleanContainerName : StagingContainerName;

            await blobStorageService.DeleteFileAsync(
                targetBlobClient,
                containerName: targetContainer,
                blobPath: doc.DocumentStoragePath
            );
        }

        // Handle single vs multiple delete redirection
        if (!multipleDelete)
        {
            // Call the respondent service to fetch metadata for documents
            var response = await respondentService.GetModificationChangesDocuments(
                request.ProjectModificationId, request.ProjectRecordId);

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

    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Download)]
    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string path, string fileName)
    {
        // get the modification id from the path
        var modificationId = path.Split('/')[1];

        var documentAccessResponse = await projectModificationsService.CheckDocumentAccess(Guid.Parse(modificationId));

        if (!documentAccessResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(documentAccessResponse);
        }

        var blobClient = GetBlobClient(true);
        var serviceResponse = await blobStorageService
            .DownloadFileToHttpResponseAsync(blobClient, CleanContainerName, path, fileName);

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
    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Upload)]
    [HttpPost]
    public async Task<IActionResult> UploadDocuments(ModificationUploadDocumentsViewModel model)
    {
        // If the posted model is null (due to exceeding max request size)
        if (model?.Files == null)
            return View("FileTooLarge");

        model.ModificationId = TempData.PeekGuid(TempDataKeys.ProjectModification.ProjectModificationId);
        var respondentId = (HttpContext.Items[ContextItemKeys.UserId] as string)!;
        model.ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;
        model.IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;

        var processedModel = await ProcessDocumentUploadsAsync(
            model,
            respondentId);

        if (processedModel.HasServiceError)
            return this.ServiceError(processedModel.Response!);

        if (processedModel.ShouldRedirectToDocumentsAdded)
            return RedirectToAction(nameof(ModificationDocumentsAdded)); // or your new action

        // Only return view (as required)
        return View(processedModel);
    }

    [ModificationAuthorise(Permissions.MyResearch.ProjectDocuments_Download)]
    public async Task<IActionResult> DownloadDocumentsAsZip(string folderName)
    {
        var irasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;
        var modificationIdentifier =
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string;

        var documentAccessResponse = await projectModificationsService.CheckDocumentAccess(Guid.Parse(folderName));

        if (!documentAccessResponse.IsSuccessStatusCode)
        {
            return this.ServiceError(documentAccessResponse);
        }

        var blobClient = GetBlobClient(true);

        // Build the file name
        var saveAsFileName = BuildZipFileName(modificationIdentifier);

        var response = await blobStorageService.DownloadFolderAsZipAsync(
            blobClient,
            CleanContainerName,
            $"{irasId}/{folderName}",
            saveAsFileName);

        return File(response.FileBytes, "application/zip", response.FileName);
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
    [Authorize(Policy = Permissions.MyResearch.ProjectDocuments_Update)]
    [HttpPost]
    public async Task<IActionResult> SaveDocumentDetails
    (
        ModificationAddDocumentDetailsViewModel viewModel,
        bool saveForLater = false,
        bool reviewAllChanges = false
    )
    {
        var supersedeEnabled = await featureManager.IsEnabledAsync(FeatureFlags.SupersedingDocuments);

        var questionnaire = await BuildAndBindQuestionnaire(viewModel);

        var isValid = await ValidateDocumentDetails(viewModel, questionnaire, saveForLater);
        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        if (!isValid)
        {
            return View("AddDocumentDetails", viewModel);
        }

        var existingStatus = await GetExistingStatus(viewModel.DocumentId, questionnaire);

        SupersedeContext supersedeContext = null;

        if (supersedeEnabled)
        {
            supersedeContext = await HandleSupersedePreSave(viewModel, questionnaire);
        }

        await SaveModificationDocumentAnswers(viewModel);

        await HandleCompletionAudit(existingStatus, viewModel, questionnaire);

        if (supersedeEnabled)
        {
            await HandleDocumentTypeChange(viewModel, supersedeContext);
            await HandleSupersedeAnswerChange(viewModel, supersedeContext, reviewAllChanges);

            var redirect = HandleSupersedeRedirect(viewModel, questionnaire);
            if (redirect != null)
            {
                return redirect;
            }
        }

        if (reviewAllChanges)
        {
            return RedirectToReviewAllChanges();
        }

        return saveForLater
            ? RedirectToSaveForLater()
            : RedirectAfterSubmit(viewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectDocuments_Update)]
    [HttpGet]
    [FeatureGate(FeatureFlags.SupersedingDocuments)]
    public async Task<IActionResult> SupersedeDocumentToReplace
    (
        string documentTypeId,
        string projectRecordId,
        Guid currentDocumentId,
        bool reviewAnswers = false,
        bool reviewAllChanges = false
    )
    {
        var viewModel = await BuildSupersedeBaseViewModel(
            currentDocumentId,
            documentTypeId,
            reviewAnswers,
            reviewAllChanges);

        var eligibleDocuments = await GetEligibleDocumentsToReplace(
            projectRecordId,
            documentTypeId,
            currentDocumentId);

        viewModel.DocumentToReplaceList = ToGdsOptions(eligibleDocuments);

        return View(nameof(SupersedeDocumentToReplace), viewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectDocuments_Update)]
    [HttpGet]
    [FeatureGate(FeatureFlags.SupersedingDocuments)]
    public async Task<IActionResult> SupersedeDocumentType
    (
        string documentTypeId,
        string projectRecordId,
        Guid currentDocumentId,
        bool reviewAnswers = false,
        bool reviewAllChanges = false
    )
    {
        var viewModel = await BuildSupersedeBaseViewModel(
            currentDocumentId,
            documentTypeId,
            reviewAnswers,
            reviewAllChanges);

        return View(nameof(SupersedeDocumentType), viewModel);
    }

    [Authorize(Policy = Permissions.MyResearch.ProjectDocuments_Update)]
    [HttpGet]
    [FeatureGate(FeatureFlags.SupersedingDocuments)]
    public async Task<IActionResult> SupersedeLinkDocument
    (
        string documentTypeId,
        string projectRecordId,
        Guid currentDocumentId,
        bool reviewAnswers = false,
        bool reviewAllChanges = false,
        bool linkDocument = false
    )
    {
        var viewModel = await BuildSupersedeBaseViewModel
        (
            currentDocumentId,
            documentTypeId,
            reviewAllChanges,
            reviewAnswers
        );

        var eligibleDocuments = await GetEligibleDocumentsToLink(currentDocumentId);

        viewModel.DocumentToReplaceList = ToGdsOptions(eligibleDocuments);

        return View(nameof(SupersedeLinkDocument), viewModel);
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
    [Authorize(Policy = Permissions.MyResearch.ProjectDocuments_Update)]
    [HttpPost]
    [FeatureGate(FeatureFlags.SupersedingDocuments)]
    public async Task<IActionResult> SaveSupersedeDocumentDetails
    (
        ModificationAddDocumentDetailsViewModel viewModel,
        bool saveForLater = false,
        bool continueToDocumentType = false,
        bool continueToLinkDocument = false,
        bool linkDocument = false,
        bool reviewAnswers = false
    )
    {
        var userId = HttpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty;

        // Fetch current document details
        var replacedByDocumentResponse =
            await respondentService.GetModificationDocumentDetails(viewModel.DocumentId);

        var projectModificationDocumentRequest = new List<ProjectModificationDocumentRequest>();

        // Handle file upload (if present)
        var newLinkedDocumentId = await UploadLinkedDocumentIfRequired(viewModel, userId, linkDocument);

        var hasFileUpload = newLinkedDocumentId.HasValue;

        var hasReplacesDocumentChanged =
            replacedByDocumentResponse.Content?.ReplacesDocumentId.HasValue == true &&
            replacedByDocumentResponse.Content?.ReplacesDocumentId.Value != Guid.Empty &&
            replacedByDocumentResponse.Content?.ReplacesDocumentId != viewModel.ReplacesDocumentId;

        // Build primary document DTO (same logic as original)
        var replacedByDocumentDto = new ProjectModificationDocumentRequest
        {
            Id = viewModel.DocumentId,
            ProjectModificationId = replacedByDocumentResponse.Content?.ProjectModificationId ?? default,
            ProjectRecordId = replacedByDocumentResponse.Content?.ProjectRecordId,
            UserId = replacedByDocumentResponse.Content?.UserId,
            FileName = viewModel.FileName,
            DocumentStoragePath = viewModel.DocumentStoragePath,
            FileSize = viewModel.FileSize,
            Status = replacedByDocumentResponse.Content?.Status,
            IsMalwareScanSuccessful = viewModel.IsMalwareScanSuccessful,
            ReplacesDocumentId = viewModel.ReplacesDocumentId,
            DocumentType = viewModel.DocumentType,
            ReplacedByDocumentId = viewModel.ReplacedByDocumentId,
            LinkedDocumentId = hasFileUpload
                ? newLinkedDocumentId
                : viewModel.LinkedDocumentId
        };

        projectModificationDocumentRequest.Add(replacedByDocumentDto);

        if (hasReplacesDocumentChanged)
        {
            var oldReplacedByDocumentResponse =
            await respondentService.GetModificationDocumentDetails((Guid)(replacedByDocumentResponse.Content?.ReplacesDocumentId.Value));

            if (oldReplacedByDocumentResponse.Content?.ReplacedByDocumentId != null &&
                oldReplacedByDocumentResponse.Content.ReplacedByDocumentId != Guid.Empty)
            {
                projectModificationDocumentRequest.Add(new ProjectModificationDocumentRequest
                {
                    Id = oldReplacedByDocumentResponse.Content.Id,
                    ProjectModificationId = oldReplacedByDocumentResponse.Content.ProjectModificationId,
                    ProjectRecordId = oldReplacedByDocumentResponse.Content.ProjectRecordId,
                    UserId = oldReplacedByDocumentResponse.Content.UserId,
                    FileName = oldReplacedByDocumentResponse.Content.FileName,
                    DocumentStoragePath = oldReplacedByDocumentResponse.Content.DocumentStoragePath,
                    FileSize = oldReplacedByDocumentResponse.Content.FileSize,
                    Status = oldReplacedByDocumentResponse.Content.Status,
                    IsMalwareScanSuccessful = oldReplacedByDocumentResponse.Content.IsMalwareScanSuccessful,
                    DocumentType = oldReplacedByDocumentResponse.Content.DocumentType,
                    ReplacesDocumentId = oldReplacedByDocumentResponse.Content.ReplacesDocumentId,
                    ReplacedByDocumentId = null, // Clear the ReplacedByDocumentId to unlink it from the current document
                    LinkedDocumentId = oldReplacedByDocumentResponse.Content.LinkedDocumentId
                });
            }
        }

        // Update the document being replaced (if exists)
        await AppendReplacesDocumentIfExists
            (
                replacedByDocumentDto,
                projectModificationDocumentRequest,
                userId
            );

        // Update linked document (if exists)
        await AppendLinkedDocumentIfExists
            (
            replacedByDocumentDto,
            replacedByDocumentResponse,
            projectModificationDocumentRequest,
            userId
            );

        await respondentService.SaveModificationDocuments(projectModificationDocumentRequest);

        // Sync metadata answers if linking document
        if (linkDocument &&
            replacedByDocumentDto.LinkedDocumentId is Guid linkedId &&
            linkedId != Guid.Empty)
        {
            await SyncLinkedDocumentAnswers(
                replacedByDocumentDto.Id,
                linkedId,
                replacedByDocumentDto.DocumentType ?? string.Empty);
        }

        // Navigation logic (UNCHANGED)
        if (continueToDocumentType && replacedByDocumentDto.LinkedDocumentId == null)
        {
            return reviewAnswers
                ? RedirectAfterSubmit(viewModel)
                : RedirectToAction(nameof(SupersedeDocumentType), new
                {
                    documentTypeId = viewModel.MetaDataDocumentTypeId,
                    projectRecordId = viewModel.ProjectRecordId,
                    currentDocumentId = viewModel.DocumentId
                });
        }

        if (continueToLinkDocument && replacedByDocumentDto.LinkedDocumentId == null)
        {
            return reviewAnswers
                ? RedirectAfterSubmit(viewModel)
                : RedirectToAction(nameof(SupersedeLinkDocument), new
                {
                    documentTypeId = viewModel.MetaDataDocumentTypeId,
                    projectRecordId = viewModel.ProjectRecordId,
                    currentDocumentId = viewModel.DocumentId
                });
        }

        return saveForLater
            ? RedirectToSaveForLater()
            : RedirectAfterSubmit(viewModel);
    }

    private async Task SaveLinkedDocumentAnswers
    (
        IEnumerable<ProjectModificationDocumentAnswerDto> currentAnswers,
        IEnumerable<ProjectModificationDocumentAnswerDto> existingLinkedAnswers,
        string documentType,
        Guid linkedId
    )
    {
        // Create lookup for fast matching
        var linkedAnswersLookup = existingLinkedAnswers
            .ToDictionary(a => a.QuestionId, StringComparer.OrdinalIgnoreCase);

        var mergedAnswers = new List<ProjectModificationDocumentAnswerDto>();

        foreach (var current in currentAnswers)
        {
            bool applyToLinked = false;
            var transformedAnswerText = string.Empty;

            transformedAnswerText =
            string.Equals(current.QuestionId,
                QuestionIds.DocumentName,
                StringComparison.OrdinalIgnoreCase)
                ? $"{current.AnswerText ?? string.Empty} (TC)"
                : current.AnswerText;

            // Apply transformation rules
            if (string.Equals(documentType, SupersedeDocumentsType.Clean, StringComparison.OrdinalIgnoreCase))
            {
                applyToLinked = true;
            }
            else if (string.Equals(documentType, SupersedeDocumentsType.Tracked, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(current.QuestionId, QuestionIds.DocumentName, StringComparison.OrdinalIgnoreCase))
                {
                    var currentDto = new ProjectModificationDocumentAnswerDto
                    {
                        AnswerText = transformedAnswerText,
                        Answers = [],
                        CategoryId = current.CategoryId,
                        Id = current.Id,
                        ModificationDocumentId = current.ModificationDocumentId,
                        OptionType = current.OptionType,
                        QuestionId = current.QuestionId,
                        SelectedOption = current.SelectedOption,
                        SectionId = current.SectionId,
                        VersionId = current.VersionId
                    };
                    mergedAnswers.Add(currentDto);
                }
            }

            if (linkedAnswersLookup.TryGetValue(current.QuestionId, out var existing))
            {
                // UPDATE existing answer
                existing.AnswerText = applyToLinked ? transformedAnswerText : current.AnswerText;
                existing.SelectedOption = current.SelectedOption;
                existing.VersionId = current.VersionId ?? string.Empty;
                existing.CategoryId = current.CategoryId;
                existing.SectionId = current.SectionId;
                existing.OptionType = current.OptionType;
                existing.Answers = current.Answers ?? [];

                mergedAnswers.Add(existing);
            }
            else
            {
                // INSERT new answer
                mergedAnswers.Add(new ProjectModificationDocumentAnswerDto
                {
                    Id = Guid.NewGuid(),
                    ModificationDocumentId = linkedId,
                    QuestionId = current.QuestionId,
                    VersionId = current.VersionId ?? string.Empty,
                    AnswerText = applyToLinked ? transformedAnswerText : current.AnswerText,
                    CategoryId = current.CategoryId,
                    SectionId = current.SectionId,
                    SelectedOption = current.SelectedOption,
                    OptionType = current.OptionType,
                    Answers = current.Answers ?? []
                });
            }
        }

        if (mergedAnswers.Count > 0)
        {
            await respondentService.SaveModificationDocumentAnswers(mergedAnswers);
        }
    }

    private (string ProjectRecordId,
         string ShortTitle,
         string IrasId,
         string ModificationIdentifier,
         Guid ModificationId) GetProjectContext()
    {
        return (
            TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is Guid id
                ? id
                : Guid.NewGuid()
        );
    }

    private ModificationAddDocumentDetailsViewModel BuildBaseViewModel
    (
        ProjectModificationDocumentRequest document,
        bool reviewAnswers,
        bool reviewAllChanges,
        string? selectedDocumentTypeId = null
    )
    {
        var context = GetProjectContext();

        return new ModificationAddDocumentDetailsViewModel
        {
            ProjectRecordId = context.ProjectRecordId,
            ShortTitle = context.ShortTitle,
            IrasId = context.IrasId,
            ModificationIdentifier = context.ModificationIdentifier,
            ModificationId = context.ModificationId,
            DocumentId = document.Id,
            FileName = document.FileName,
            FileSize = document.FileSize ?? 0,
            DocumentStoragePath = document.DocumentStoragePath,
            ReviewAnswers = reviewAnswers,
            ReviewAllChanges = reviewAllChanges,
            IsMalwareScanSuccessful = document.IsMalwareScanSuccessful,
            Status = document.Status,
            ReplacedByDocumentId = document.ReplacedByDocumentId,
            ReplacesDocumentId = document.ReplacesDocumentId,
            DocumentType = document.DocumentType,
            LinkedDocumentId = document.LinkedDocumentId,
            MetaDataDocumentTypeId = selectedDocumentTypeId
        };
    }

    private static List<GdsOption> ToGdsOptions(IEnumerable<ProjectModificationDocumentRequest> documents)
    {
        return documents.Select(d => new GdsOption
        {
            Value = d.Id.ToString(),
            Label = d.FileName
        }).ToList();
    }

    private static string BuildZipFileName(string? modificationIdentifier)
    {
        if (string.IsNullOrWhiteSpace(modificationIdentifier))
            return $"Documents-{DateTime.UtcNow:ddMMMyy}";

        // Replace the slash with a dash
        var cleanIdentifier = modificationIdentifier.Replace("/", "-");

        // Append today's date
        return $"{cleanIdentifier}-{DateTime.UtcNow:ddMMMyy}";
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
                Status = a.Status ?? string.Empty,
                IsMalwareScanSuccessful = a.IsMalwareScanSuccessful
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

    private (
    List<IFormFile> ValidFiles,
    List<ModificationDocumentsAuditTrailDto> FailedAuditEvents,
    List<ModificationDocumentsAuditTrailDto> SuccessAuditEvents,
    bool HasErrors
    ) ValidateUploadedFiles
        (
        IEnumerable<IFormFile> files,
        List<ProjectModificationDocumentRequest> existingDocs,
        Guid projectModificationId,
        string user
        )
    {
        var validFiles = new List<IFormFile>();
        var auditEvents = new List<ModificationDocumentsAuditTrailDto>();
        var successAuditEvents = new List<ModificationDocumentsAuditTrailDto>();

        const long maxFileSize = 100 * 1024 * 1024; // 100 MB
        long totalFileSize = 0;
        bool hasErrors = false;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(fileName);

            // 1. Extension check
            if (!FileConstants.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                hasErrors = true;

                ModelState.AddModelError("Files", "The selected file must be a permitted file type");

                auditEvents.Add(CreateAuditTrail(
                    projectModificationId,
                    fileName,
                    DocumentAuditEvents.UploadFailedUnsupported,
                    user));

                continue;
            }

            // 2. Duplicate file check
            if (existingDocs.Any(d =>
                d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                hasErrors = true;

                ModelState.AddModelError("Files", $"{fileName} has already been uploaded");

                auditEvents.Add(CreateAuditTrail(
                    projectModificationId,
                    fileName,
                    DocumentAuditEvents.UploadFailedDuplicate,
                    user));

                continue;
            }

            // 3. Individual file size check
            if (file.Length > maxFileSize)
            {
                hasErrors = true;

                ModelState.AddModelError("Files", $"{fileName} must be smaller than 100 MB");

                auditEvents.Add(CreateAuditTrail(
                    projectModificationId,
                    fileName,
                    DocumentAuditEvents.UploadFailedFileSize,
                    user));

                continue;
            }

            totalFileSize += file.Length;
            validFiles.Add(file);

            // Prepare success audit entry (persist later)
            successAuditEvents.Add(CreateAuditTrail(
                projectModificationId,
                fileName,
                DocumentAuditEvents.UploadSuccessful,
                user));
        }

        // 4. Combined file size check
        if (totalFileSize > maxFileSize)
        {
            hasErrors = true;

            ModelState.AddModelError("Files", "The combined size of all files must be less than 100 MB");

            validFiles.Clear(); // none should be uploaded
            successAuditEvents.Clear();
        }

        return (validFiles, auditEvents, successAuditEvents, hasErrors);
    }

    private async Task<List<ProjectModificationDocumentRequest>> UploadValidFilesAsync
    (
        List<IFormFile> validFiles,
        string irasId,
        object? projectModificationId,
        string projectRecordId,
        string respondentId
    )
    {
        var blobClient = GetBlobClient(false);

        // Upload only valid files to blob storage
        var uploadedBlobs = await blobStorageService.UploadFilesAsync(blobClient, validFiles, StagingContainerName, $"{irasId}/{projectModificationId}");

        // Map uploaded blob metadata to DTOs for backend service
        var uploadedDocuments = uploadedBlobs.ConvertAll(blob => new ProjectModificationDocumentRequest
        {
            ProjectModificationId = projectModificationId == null ? Guid.Empty : (Guid)projectModificationId!,
            ProjectRecordId = projectRecordId,
            UserId = respondentId,
            FileName = blob.FileName,
            DocumentStoragePath = blob.BlobUri,
            FileSize = blob.FileSize,
            Status = DocumentStatus.Uploaded
        });

        await projectModificationsService.CreateDocumentModification(uploadedDocuments);
        return uploadedDocuments;
    }

    private static List<DocumentSummaryItemDto> AppendAndSortDocuments
    (
        List<DocumentSummaryItemDto>? existing,
        List<ProjectModificationDocumentRequest> uploaded
    )
    {
        // Append newly uploaded documents to existing ones, ensuring consistent alphabetical order
        return [.. (existing ?? [])
            .Concat(uploaded.Select(a => new DocumentSummaryItemDto
            {
                DocumentId = a.Id,
                FileName = a.FileName,
                FileSize = a.FileSize ?? 0,
                BlobUri = a.DocumentStoragePath,
                Status = a.Status ?? string.Empty,
                IsMalwareScanSuccessful = a.IsMalwareScanSuccessful
            }))
            .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase)];
    }

    private IActionResult HandleNoNewFiles
    (
        ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>? response,
        bool atleastOneInvalidFile,
        ModificationUploadDocumentsViewModel model
    )
    {
        if (atleastOneInvalidFile)
        {
            return View(model);
        }

        if (response?.StatusCode != HttpStatusCode.OK)
        {
            return this.ServiceError(response!);
        }

        if (response.Content?.Any() == true)
        {
            return RedirectToAction(nameof(ModificationDocumentsAdded));
        }

        ModelState.AddModelError("Files", "Upload at least one document");
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
            documentChangeRequest.ProjectRecordId);

        // Retrieve the CMS question set for the document details section
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        var cmsQuestions = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);
        var viewModels = new List<ModificationAddDocumentDetailsViewModel>();
        var questionIndex = 0;

        foreach (var doc in response!.Content
            .Where(d => !(d.DocumentType == SupersedeDocumentsType.Tracked
                  && d.LinkedDocumentId.HasValue
                  && d.LinkedDocumentId.Value != Guid.Empty))
            .OrderBy(d => d.FileName, StringComparer.OrdinalIgnoreCase))
        {
            // Fetch existing answers for this document
            var answersResponse = await respondentService.GetModificationDocumentAnswers(doc.Id);
            var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                ? answersResponse.Content ?? [] : [];

            var previousVersionOfDocumentAnswer = answers
                .FirstOrDefault(q =>
                    string.Equals(
                        q.QuestionId,
                        QuestionIds.PreviousVersionOfDocument,
                        StringComparison.OrdinalIgnoreCase))
                ?.SelectedOption;

            var documentTypeId = answers
                    .FirstOrDefault(x => x.QuestionId.Equals(QuestionIds.SelectedDocumentType, StringComparison.OrdinalIgnoreCase))?.SelectedOption;

            var documentToReplaceFileName = string.Empty;
            var linkedDocumentFileName = string.Empty;
            var linkedDocumentStoragePath = string.Empty;
            var documentToReplaceStoragePath = string.Empty;
            if (doc.ReplacesDocumentId != null)
            {
                var replacesDocumentAnswersResponse = await respondentService.GetModificationDocumentDetails(doc.ReplacesDocumentId.Value);
                var replacesDocumentAnswers = replacesDocumentAnswersResponse?.StatusCode == HttpStatusCode.OK
                    ? replacesDocumentAnswersResponse.Content ?? new ProjectModificationDocumentRequest()
                    : new ProjectModificationDocumentRequest();
                documentToReplaceFileName = replacesDocumentAnswers.FileName ?? string.Empty;
                documentToReplaceStoragePath = replacesDocumentAnswers.DocumentStoragePath ?? string.Empty;
            }

            if (doc.LinkedDocumentId != null)
            {
                var linkedDocumentAnswersResponse = await respondentService.GetModificationDocumentDetails(doc.LinkedDocumentId.Value);
                var linkedDocumentAnswers = linkedDocumentAnswersResponse?.StatusCode == HttpStatusCode.OK
                    ? linkedDocumentAnswersResponse.Content ?? new ProjectModificationDocumentRequest()
                    : new ProjectModificationDocumentRequest();
                linkedDocumentFileName = linkedDocumentAnswers.FileName ?? string.Empty;
                linkedDocumentStoragePath = linkedDocumentAnswers.DocumentStoragePath ?? string.Empty;
            }

            // Map document and questions into a view model
            var vm = new ModificationAddDocumentDetailsViewModel
            {
                DocumentId = doc.Id,
                FileName = doc.FileName,
                DocumentStoragePath = doc.DocumentStoragePath,
                ProjectRecordId = doc.ProjectRecordId,
                Status = doc.Status,
                IsMalwareScanSuccessful = doc.IsMalwareScanSuccessful,
                ReviewAnswers = true,
                ReplacesDocumentId = doc.ReplacesDocumentId,
                ReplacedByDocumentId = doc.ReplacedByDocumentId,
                LinkedDocumentId = doc.LinkedDocumentId,
                DocumentType = doc.DocumentType,
                DocumentToReplaceFileName = documentToReplaceFileName,
                DocumentToReplaceStoragePath = documentToReplaceStoragePath,
                LinkedDocumentFileName = linkedDocumentFileName,
                LinkedDocumentStoragePath = linkedDocumentStoragePath,
                ShowSupersedeDocumentSection = string.Equals(previousVersionOfDocumentAnswer, QuestionIds.PreviousVersionOfDocumentYesOption, StringComparison.OrdinalIgnoreCase),
                MetaDataDocumentTypeId = documentTypeId,

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
    private async Task<bool> ValidateDuplicateDocumentNames
    (
        ModificationAddDocumentDetailsViewModel viewModel,
        dynamic? documentNameQuestion
    )
    {
        var request = BuildDocumentRequest();

        // Fetch all existing uploaded documents for this modification
        var documentsResponse = await respondentService.GetModificationChangesDocuments(
            request.ProjectModificationId,
            request.ProjectRecordId);

        // Skip validation if service call fails or no documents exist
        if (documentsResponse?.StatusCode != HttpStatusCode.OK || documentsResponse.Content == null)
        {
            return true;
        }

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
        TempData[TempDataKeys.ShowNotificationBanner] = true;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        var status = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) as string;
        var sponsorOrganisationUserId = TempData.Peek(TempDataKeys.RevisionSponsorOrganisationUserId);
        var rtsId = TempData.Peek(TempDataKeys.RevisionRtsId) as string;
        if (status is ModificationStatus.ReviseAndAuthorise)
        {
            return RedirectToRoute("sws:modifications", new { sponsorOrganisationUserId, rtsId });
        }
        return RedirectToRoute(PostApprovalRoute, new { projectRecordId });
    }

    private RedirectToRouteResult RedirectToReviewAllChanges()
    {
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string;
        var shortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty;
        var irasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;
        var modificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty;
        var projectModificationId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationId) is Guid modId
               ? modId
               : Guid.NewGuid();
        return RedirectToRoute(ReviewAllChangesRoute, new { projectRecordId, irasId, shortTitle, projectModificationId });
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

    // Helper method to get correct blob client
    private BlobServiceClient GetBlobClient(bool useCleanContainer)
    {
        var name = useCleanContainer ? "Clean" : "Staging";
        return _blobClientFactory.CreateClient(name);
    }

    private static ModificationDocumentsAuditTrailDto CreateAuditTrail
    (
        Guid projectModificationId,
        string fileName,
        string description,
        string user
    )
    {
        return new ModificationDocumentsAuditTrailDto
        {
            Id = Guid.NewGuid(),
            ProjectModificationId = projectModificationId,
            DateTimeStamp = DateTime.UtcNow,
            Description = description,
            FileName = fileName,
            User = user
        };
    }

    private async Task<ModificationUploadDocumentsViewModel> ProcessDocumentUploadsAsync
    (
        ModificationUploadDocumentsViewModel model,
        string respondentId,
        bool linkDocument = false
    )
    {
        var auditEntries = new List<ModificationDocumentsAuditTrailDto>();

        // Fetch existing documents
        var response = await respondentService.GetModificationChangesDocuments(
            model.ModificationId == null ? Guid.Empty : Guid.Parse(model.ModificationId),
            model.ProjectRecordId);
        model.Response = response!;

        // Map and validate existing documents
        var atleastOneInvalidFile = false;

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            model.UploadedDocuments = MapDocuments(response.Content);
            atleastOneInvalidFile = ValidateExistingDocuments(model);
        }

        // If no new files selected, just return model (preserves existing behaviour)
        if (model.Files == null || model.Files.Count == 0)
        {
            if (atleastOneInvalidFile)
            {
                model.ShouldReturnView = true;
                return model;
            }

            if (response?.StatusCode != HttpStatusCode.OK)
            {
                model.HasServiceError = true;
                return model;
            }

            if (response.Content?.Any() == true)
            {
                model.ShouldRedirectToDocumentsAdded = true;
                return model;
            }

            ModelState.AddModelError("Files", "Please upload at least one document");
            model.ShouldReturnView = true;
            return model;
        }

        // Validate uploaded files
        var validationResult = ValidateUploadedFiles(
            model.Files,
            response?.Content?.ToList() ?? [],
            model.ModificationId == null ? Guid.Empty : Guid.Parse(model.ModificationId),
            respondentId);

        // Persist FAILED audit events
        if (validationResult.FailedAuditEvents.Count > 0)
        {
            await projectModificationsService
                .CreateModificationDocumentsAuditTrail(validationResult.FailedAuditEvents);
        }

        atleastOneInvalidFile = validationResult.HasErrors;

        // Check for 100MB specific error (UI rule preserved)
        var hasFileSizeError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Any(e => e.ErrorMessage.Contains("100 MB", StringComparison.OrdinalIgnoreCase));

        // If validation failed due to file size, return immediately (no upload)
        if (atleastOneInvalidFile && hasFileSizeError)
        {
            return model;
        }

        // Upload valid files
        if (validationResult.ValidFiles.Count > 0)
        {
            var uploadedDocuments = await UploadValidFilesAsync(
                validationResult.ValidFiles,
                model.IrasId,
                model.ModificationId == null ? Guid.Empty : Guid.Parse(model.ModificationId),
                model.ProjectRecordId,
                respondentId);

            // Persist SUCCESS audit events AFTER successful upload
            if (validationResult.SuccessAuditEvents.Count > 0)
            {
                await projectModificationsService
                    .CreateModificationDocumentsAuditTrail(validationResult.SuccessAuditEvents);
            }

            // Append and sort documents
            if (linkDocument)
            {
                model.UploadedDocuments = [];
            }

            model.UploadedDocuments = AppendAndSortDocuments(
                model.UploadedDocuments,
                uploadedDocuments);
        }

        return model;
    }

    private async Task<QuestionnaireViewModel> BuildAndBindQuestionnaire(ModificationAddDocumentDetailsViewModel viewModel)
    {
        var questionnaire = await BuildUpdatedQuestionnaire(viewModel);
        viewModel.Questions = questionnaire.Questions;
        return questionnaire;
    }

    private async Task<bool> ValidateDocumentDetails
        (
    ModificationAddDocumentDetailsViewModel viewModel,
    QuestionnaireViewModel questionnaire,
    bool saveForLater
        )
    {
        var isValid = await this.ValidateQuestionnaire(validator, viewModel);

        if (!saveForLater)
            isValid &= ValidateRequiredDates(viewModel);

        var documentNameQuestion = questionnaire.Questions
            .Select((q, i) => new { Question = q, Index = i })
            .FirstOrDefault(x => x.Question.QuestionId.Equals(
                QuestionIds.DocumentName,
                StringComparison.OrdinalIgnoreCase));

        isValid &= await ValidateDuplicateDocumentNames(viewModel, documentNameQuestion);

        return isValid;
    }

    private async Task<DocumentDetailStatus> GetExistingStatus(Guid documentId, QuestionnaireViewModel questionnaire)
    {
        var isIncomplete = await EvaluateDocumentCompletion(documentId, questionnaire);
        return isIncomplete
            ? DocumentDetailStatus.Incomplete
            : DocumentDetailStatus.Complete;
    }

    private async Task<SupersedeContext> HandleSupersedePreSave
    (
        ModificationAddDocumentDetailsViewModel viewModel,
        QuestionnaireViewModel questionnaire
    )
    {
        var answersResponse = await respondentService
            .GetModificationDocumentAnswers(viewModel.DocumentId);

        var currentAnswers = answersResponse?.StatusCode == HttpStatusCode.OK
            ? answersResponse.Content ?? []
            : [];

        var previousAnswer = currentAnswers
            .FirstOrDefault(a => a.QuestionId.Equals(
                QuestionIds.PreviousVersionOfDocument,
                StringComparison.OrdinalIgnoreCase))?.SelectedOption;

        var latestAnswer = questionnaire.Questions
            .FirstOrDefault(q => q.QuestionId.Equals(
                QuestionIds.PreviousVersionOfDocument,
                StringComparison.OrdinalIgnoreCase))?.SelectedOption;

        var oldDocumentType = currentAnswers
            .FirstOrDefault(a => a.QuestionId.Equals(
                QuestionIds.SelectedDocumentType,
                StringComparison.OrdinalIgnoreCase))?.SelectedOption;

        var newDocumentType = questionnaire.Questions
            .FirstOrDefault(q => q.QuestionId.Equals(
                QuestionIds.SelectedDocumentType,
                StringComparison.OrdinalIgnoreCase))?.SelectedOption;

        if (viewModel.LinkedDocumentId != null)
        {
            var linkedAnswersResponse = await respondentService
                .GetModificationDocumentAnswers(viewModel.LinkedDocumentId.Value);

            var existingLinkedAnswers = linkedAnswersResponse?.StatusCode == HttpStatusCode.OK
                ? linkedAnswersResponse.Content ?? []
                : [];

            await SaveLinkedDocumentAnswers(
                currentAnswers,
                existingLinkedAnswers,
                viewModel.DocumentType ?? string.Empty,
                viewModel.LinkedDocumentId.Value);
        }

        return new SupersedeContext(previousAnswer, latestAnswer, oldDocumentType, newDocumentType);
    }

    private sealed record SupersedeContext(
    string PreviousAnswer,
    string LatestAnswer,
    string OldDocumentType,
    string NewDocumentType);

    private async Task HandleCompletionAudit
    (
        DocumentDetailStatus existingStatus,
        ModificationAddDocumentDetailsViewModel viewModel,
        QuestionnaireViewModel questionnaire
    )
    {
        if (existingStatus == DocumentDetailStatus.Complete)
        {
            return;
        }

        var isIncomplete = await EvaluateDocumentCompletion(viewModel.DocumentId, questionnaire);
        var newStatus = isIncomplete
            ? DocumentDetailStatus.Incomplete
            : DocumentDetailStatus.Complete;

        if (newStatus != DocumentDetailStatus.Complete)
        {
            return;
        }

        await projectModificationsService.CreateModificationDocumentsAuditTrail(
        [
            new()
        {
            Id = Guid.NewGuid(),
            ProjectModificationId = viewModel.ModificationId,
            DateTimeStamp = DateTime.UtcNow,
            Description = DocumentAuditEvents.DocumentDetailsCompleted,
            FileName = viewModel.FileName ?? string.Empty,
            User = HttpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty
        }
        ]);
    }

    private IActionResult HandleSupersedeRedirect(ModificationAddDocumentDetailsViewModel viewModel, QuestionnaireViewModel questionnaire)
    {
        var previousVersionQuestion = questionnaire.Questions
            .FirstOrDefault(q => q.QuestionId.Equals(
                QuestionIds.PreviousVersionOfDocument,
                StringComparison.OrdinalIgnoreCase));

        var supersede = string.Equals(
            previousVersionQuestion?.SelectedOption,
            QuestionIds.PreviousVersionOfDocumentYesOption,
            StringComparison.OrdinalIgnoreCase);

        if (!supersede)
        {
            return null;
        }

        var documentTypeId = questionnaire.Questions
            .FirstOrDefault(q => q.QuestionId.Equals(
                QuestionIds.SelectedDocumentType,
                StringComparison.OrdinalIgnoreCase))?.SelectedOption;

        return viewModel.ReviewAnswers
            ? RedirectAfterSubmit(viewModel)
            : RedirectToAction(nameof(SupersedeDocumentToReplace), new
            {
                documentTypeId,
                projectRecordId = viewModel.ProjectRecordId,
                currentDocumentId = viewModel.DocumentId
            });
    }

    private async Task HandleDocumentTypeChange(ModificationAddDocumentDetailsViewModel viewModel, SupersedeContext context)
    {
        if (context == null)
        {
            return;
        }

        var hasDocumentTypeChanged =
            !string.IsNullOrWhiteSpace(context.NewDocumentType) &&
            !string.Equals(
                context.OldDocumentType,
                context.NewDocumentType,
                StringComparison.OrdinalIgnoreCase);

        if (!hasDocumentTypeChanged)
        {
            return;
        }

        var requests = new List<ProjectModificationDocumentRequest>();
        var userId = HttpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty;

        // Fetch current document details once
        var currentDocumentResponse =
            await respondentService.GetModificationDocumentDetails(viewModel.DocumentId);

        if (currentDocumentResponse?.Content == null)
        {
            return;
        }

        // Update current document (clear superseding relationship)
        requests.Add(BuildProjectModificationDocumentRequest(
            currentDocumentResponse.Content,
            viewModel,
            userId,
            replacesDocumentId: null,
            replacedByDocumentId: null));

        // If it was replacing another document, clear that relationship too
        if (viewModel.ReplacesDocumentId != null && viewModel.ReplacesDocumentId != Guid.Empty)
        {
            var replacedDocResponse =
                await respondentService.GetModificationDocumentDetails(viewModel.ReplacesDocumentId.Value);

            if (replacedDocResponse?.StatusCode == HttpStatusCode.OK &&
                replacedDocResponse.Content != null)
            {
                requests.Add(BuildProjectModificationDocumentRequest(
                    replacedDocResponse.Content,
                    viewModel,
                    userId,
                    replacesDocumentId: null,
                    replacedByDocumentId: null));
            }
        }

        if (requests.Count > 0)
        {
            await respondentService.SaveModificationDocuments(requests);
        }
    }

    private async Task HandleSupersedeAnswerChange
    (
        ModificationAddDocumentDetailsViewModel viewModel,
        SupersedeContext context,
        bool reviewAllChanges
    )
    {
        if (context == null ||
            string.IsNullOrEmpty(context.PreviousAnswer) ||
            string.IsNullOrEmpty(context.LatestAnswer))
            return;

        var changedFromYesToNo =
            string.Equals(context.PreviousAnswer,
                QuestionIds.PreviousVersionOfDocumentYesOption,
                StringComparison.OrdinalIgnoreCase)
            &&
            string.Equals(context.LatestAnswer,
                QuestionIds.PreviousVersionOfDocumentNoOption,
                StringComparison.OrdinalIgnoreCase);

        if (!changedFromYesToNo)
        {
            return;
        }

        var userId = HttpContext.Items[ContextItemKeys.UserId]?.ToString() ?? string.Empty;
        var requests = new List<ProjectModificationDocumentRequest>();

        // Get fresh document details
        var currentDocumentResponse =
            await respondentService.GetModificationDocumentDetails(viewModel.DocumentId);

        if (currentDocumentResponse?.Content == null)
        {
            return;
        }

        // Rebuild base view model (preserves original behaviour)
        viewModel = BuildBaseViewModel(
            currentDocumentResponse.Content,
            viewModel.ReviewAnswers,
            reviewAllChanges,
            null);

        var replacesDocumentId = viewModel.ReplacesDocumentId;

        // 1. Clear linked document relationship
        if (viewModel.LinkedDocumentId != null)
        {
            var linkedDocResponse =
                await respondentService.GetModificationDocumentDetails(viewModel.LinkedDocumentId.Value);

            if (linkedDocResponse?.StatusCode == HttpStatusCode.OK &&
                linkedDocResponse.Content != null)
            {
                var linkedDto = new ProjectModificationDocumentRequest
                {
                    Id = linkedDocResponse.Content.Id,
                    ProjectModificationId = viewModel.ModificationId,
                    ProjectRecordId = viewModel.ProjectRecordId,
                    UserId = userId,
                    FileName = linkedDocResponse.Content.FileName,
                    DocumentStoragePath = linkedDocResponse.Content.DocumentStoragePath,
                    FileSize = linkedDocResponse.Content.FileSize,
                    Status = linkedDocResponse.Content.Status,
                    IsMalwareScanSuccessful = linkedDocResponse.Content.IsMalwareScanSuccessful,
                    ReplacesDocumentId = linkedDocResponse.Content.ReplacesDocumentId,
                    DocumentType = null,
                    LinkedDocumentId = null
                };

                requests.Add(linkedDto);

                // Delete linked document answers (existing behaviour)
                await projectModificationsService.DeleteDocumentAnswersModification([linkedDto]);
            }
        }

        // 2. Clear replaced document relationship
        if (replacesDocumentId != null && replacesDocumentId != Guid.Empty)
        {
            var replacedDocResponse =
                await respondentService.GetModificationDocumentDetails(replacesDocumentId.Value);

            if (replacedDocResponse?.StatusCode == HttpStatusCode.OK &&
                replacedDocResponse.Content != null)
            {
                requests.Add(BuildProjectModificationDocumentRequest(
                    replacedDocResponse.Content,
                    viewModel,
                    userId,
                    replacesDocumentId: null,
                    replacedByDocumentId: null));
            }
        }

        // 3. Reset current document supersede fields
        viewModel.DocumentType = null;
        viewModel.LinkedDocumentId = null;
        viewModel.ReplacedByDocumentId = null;
        viewModel.ReplacesDocumentId = null;

        requests.Add(new ProjectModificationDocumentRequest
        {
            Id = viewModel.DocumentId,
            ProjectModificationId = viewModel.ModificationId,
            ProjectRecordId = viewModel.ProjectRecordId,
            UserId = userId,
            FileName = viewModel.FileName,
            DocumentStoragePath = viewModel.DocumentStoragePath,
            FileSize = viewModel.FileSize,
            Status = viewModel.Status,
            IsMalwareScanSuccessful = viewModel.IsMalwareScanSuccessful,
            ReplacesDocumentId = null,
            DocumentType = null,
            ReplacedByDocumentId = null,
            LinkedDocumentId = null
        });

        if (requests.Count > 0)
        {
            await respondentService.SaveModificationDocuments(requests);
        }
    }

    private ProjectModificationDocumentRequest BuildProjectModificationDocumentRequest
    (
        ProjectModificationDocumentRequest source,
        ModificationAddDocumentDetailsViewModel viewModel,
        string userId,
        Guid? replacesDocumentId,
        Guid? replacedByDocumentId
    )
    {
        return new ProjectModificationDocumentRequest
        {
            Id = source.Id,
            ProjectModificationId = source.ProjectModificationId,
            ProjectRecordId = source.ProjectRecordId,
            UserId = userId,
            FileName = source.FileName,
            DocumentStoragePath = source.DocumentStoragePath,
            FileSize = source.FileSize,
            Status = source.Status,
            IsMalwareScanSuccessful = source.IsMalwareScanSuccessful,
            ReplacesDocumentId = replacesDocumentId,
            DocumentType = source.DocumentType,
            ReplacedByDocumentId = replacedByDocumentId,
            LinkedDocumentId = source.LinkedDocumentId
        };
    }

    private async Task<Guid?> UploadLinkedDocumentIfRequired(ModificationAddDocumentDetailsViewModel viewModel, string userId, bool linkDocument = false)
    {
        if (viewModel.File == null)
            return null;

        var uploadViewModel = new ModificationUploadDocumentsViewModel
        {
            Files = [viewModel.File],
            ProjectRecordId = viewModel.ProjectRecordId,
            ModificationId = viewModel.ModificationId.ToString()
        };

        uploadViewModel.ModificationId =
            TempData.PeekGuid(TempDataKeys.ProjectModification.ProjectModificationId);

        uploadViewModel.ProjectRecordId =
            TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        uploadViewModel.IrasId =
            TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty;

        var updatedUploadModel = await ProcessDocumentUploadsAsync(
            uploadViewModel,
            userId,
            linkDocument);

        return updatedUploadModel.UploadedDocuments?.FirstOrDefault()?.DocumentId;
    }

    private async Task AppendReplacesDocumentIfExists
    (
        ProjectModificationDocumentRequest replacedByDocumentDto,
        List<ProjectModificationDocumentRequest> requests,
        string userId
    )
    {
        if (replacedByDocumentDto.ReplacesDocumentId == null ||
            replacedByDocumentDto.ReplacesDocumentId == Guid.Empty)
        {
            return;
        }

        var replacesDocumentResponse =
            await respondentService.GetModificationDocumentDetails(
                replacedByDocumentDto.ReplacesDocumentId.Value);

        if (replacesDocumentResponse?.StatusCode != HttpStatusCode.OK ||
            replacesDocumentResponse.Content == null)
        {
            return;
        }

        var replacesDocument = replacesDocumentResponse.Content;

        requests.Add(new ProjectModificationDocumentRequest
        {
            Id = replacesDocument.Id,
            ProjectModificationId = replacesDocument.ProjectModificationId,
            ProjectRecordId = replacesDocument.ProjectRecordId,
            UserId = userId,
            FileName = replacesDocument.FileName,
            DocumentStoragePath = replacesDocument.DocumentStoragePath,
            FileSize = replacesDocument.FileSize,
            Status = replacesDocument.Status,
            IsMalwareScanSuccessful = replacesDocument.IsMalwareScanSuccessful,
            ReplacesDocumentId = replacesDocument.ReplacesDocumentId,
            DocumentType = replacesDocument.DocumentType,
            ReplacedByDocumentId = replacedByDocumentDto.Id,
            LinkedDocumentId = replacesDocument.LinkedDocumentId
        });
    }

    private async Task AppendLinkedDocumentIfExists
    (
        ProjectModificationDocumentRequest replacedByDocumentDto,
        ServiceResponse<ProjectModificationDocumentRequest> replacedByDocumentResponse,
        List<ProjectModificationDocumentRequest> requests,
        string userId
    )
    {
        if (replacedByDocumentDto.LinkedDocumentId == null ||
            replacedByDocumentDto.LinkedDocumentId == Guid.Empty ||
            replacedByDocumentResponse?.Content == null)
        {
            return;
        }

        var linkedDocumentResponse =
            await respondentService.GetModificationDocumentDetails(
                replacedByDocumentDto.LinkedDocumentId.Value);

        if (linkedDocumentResponse?.StatusCode != HttpStatusCode.OK ||
            linkedDocumentResponse.Content == null)
        {
            return;
        }

        var linkedDocument = linkedDocumentResponse.Content;

        var linkedDocumentDto = new ProjectModificationDocumentRequest
        {
            Id = linkedDocument.Id,
            ProjectModificationId = linkedDocument.ProjectModificationId,
            ProjectRecordId = linkedDocument.ProjectRecordId,
            UserId = userId,
            FileName = linkedDocument.FileName,
            DocumentStoragePath = linkedDocument.DocumentStoragePath,
            FileSize = linkedDocument.FileSize,
            Status = linkedDocument.Status,
            IsMalwareScanSuccessful = linkedDocument.IsMalwareScanSuccessful,
            ReplacesDocumentId = linkedDocument.ReplacesDocumentId,
            DocumentType = linkedDocument.DocumentType,
            ReplacedByDocumentId = linkedDocument.ReplacedByDocumentId,
            LinkedDocumentId = replacedByDocumentDto.Id
        };

        // Preserve original Clean/Tracked swap logic EXACTLY
        if (!string.IsNullOrWhiteSpace(replacedByDocumentDto.DocumentType))
        {
            if (string.Equals(replacedByDocumentDto.DocumentType, SupersedeDocumentsType.Clean, StringComparison.OrdinalIgnoreCase))
            {
                linkedDocumentDto.DocumentType = SupersedeDocumentsType.Tracked;
            }
            else if (string.Equals(replacedByDocumentDto.DocumentType, SupersedeDocumentsType.Tracked, StringComparison.OrdinalIgnoreCase))
            {
                linkedDocumentDto.DocumentType = SupersedeDocumentsType.Clean;
                linkedDocumentDto.LinkedDocumentId = replacedByDocumentDto.Id;
                linkedDocumentDto.ReplacesDocumentId = replacedByDocumentDto.ReplacesDocumentId;
                linkedDocumentDto.ReplacedByDocumentId = replacedByDocumentDto.ReplacedByDocumentId;

                replacedByDocumentDto.LinkedDocumentId = linkedDocumentDto.Id;
                replacedByDocumentDto.ReplacesDocumentId = null;
                replacedByDocumentDto.ReplacedByDocumentId = null;
            }
        }

        requests.Add(linkedDocumentDto);
    }

    private async Task SyncLinkedDocumentAnswers
    (
        Guid currentDocumentId,
        Guid linkedDocumentId,
        string documentType
    )
    {
        var currentAnswersResponse =
            await respondentService.GetModificationDocumentAnswers(currentDocumentId);

        var currentAnswers =
            currentAnswersResponse?.StatusCode == HttpStatusCode.OK
                ? currentAnswersResponse.Content ?? []
                : [];

        var linkedAnswersResponse =
            await respondentService.GetModificationDocumentAnswers(linkedDocumentId);

        var existingLinkedAnswers =
            linkedAnswersResponse?.StatusCode == HttpStatusCode.OK
                ? linkedAnswersResponse.Content ?? []
                : [];

        await SaveLinkedDocumentAnswers(
            currentAnswers,
            existingLinkedAnswers,
            documentType,
            linkedDocumentId);
    }

    private async Task<ModificationAddDocumentDetailsViewModel> BuildSupersedeBaseViewModel
    (
        Guid currentDocumentId,
        string documentTypeId,
        bool reviewAnswers,
        bool reviewAllChanges
    )
    {
        var documentResponse =
            await respondentService.GetModificationDocumentDetails(currentDocumentId);

        return BuildBaseViewModel(
            documentResponse.Content,
            reviewAnswers,
            reviewAllChanges,
            documentTypeId);
    }

    private async Task<List<ProjectModificationDocumentRequest>> GetEligibleDocumentsToReplace
    (
        string projectRecordId,
        string documentTypeId,
        Guid currentDocumentId
    )
    {
        var documentsResponse =
            await respondentService.GetModificationDocumentsByType(
                projectRecordId,
                documentTypeId);

        return [.. (documentsResponse?.Content ?? [])
            .Where(d =>
                d.Status == DocumentStatus.Approved &&
                d.Id != currentDocumentId &&
                d.DocumentType != SupersedeDocumentsType.Tracked &&
                d.LinkedDocumentId == null &&
                (d.ReplacedByDocumentId == null || d.ReplacedByDocumentId == currentDocumentId))];
    }

    private async Task<List<ProjectModificationDocumentRequest>> GetEligibleDocumentsToLink(Guid currentDocumentId)
    {
        var documentChangeRequest = BuildDocumentRequest();

        var documentsResponse = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationId,
            documentChangeRequest.ProjectRecordId);

        return [.. (documentsResponse?.Content ?? [])
            .Where(d =>
                d.ReplacesDocumentId == null &&
                d.ReplacedByDocumentId == null &&
                d.LinkedDocumentId == null &&
                d.Id != currentDocumentId &&
                d.DocumentType != SupersedeDocumentsType.Tracked)];
    }
}