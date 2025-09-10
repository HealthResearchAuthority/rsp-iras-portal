using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators.Helpers;

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
    ) : Controller
{
    private const string ContainerName = "staging";
    private const string DocumentDetailsSection = "pdm-document-metadata";

    /// <summary>
    /// Handles GET requests for the ProjectDocument action.
    /// This action prepares the view model for uploading project documents
    /// by reading relevant metadata from TempData (such as the project modification context).
    /// </summary>
    [HttpGet]
    public IActionResult ProjectDocument()
    {
        // Populate a new upload documents view model with common project modification properties
        // (e.g., project record ID, modification ID) pulled from TempData.
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationUploadDocumentsViewModel());

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
            request.ProjectModificationChangeId, request.ProjectRecordId, request.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            // Map the backend service response into DTOs suitable for the view model.
            // Each document entry includes filename, size, and blob URI for download.
            // Documents are ordered alphabetically by filename for consistent display.
            viewModel.UploadedDocuments = [.. response.Content
            .Select(a => new DocumentSummaryItemDto
            {
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
                "No documents found or an error occurred while retrieving documents."
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
        // populate base model properties
        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationReviewDocumentsViewModel());

        // Construct the request object containing identifiers required for fetching documents.
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Fetch the CMS question set that defines the metadata/details required for each document.
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the questionnaire model from the CMS questions.
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Call the respondent service to retrieve the list of uploaded documents.
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            // For each uploaded document, fetch its associated answers and determine
            // whether details are complete or incomplete.
            var tasks = response.Content
                .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(async a =>
                {
                    // Fetch answers already provided for this document.
                    var answersResponse = await respondentService.GetModificationDocumentAnswers(a.Id);
                    var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                        ? answersResponse.Content ?? []
                        : new List<ProjectModificationDocumentAnswerDto>();

                    // Mark as incomplete if no answers exist or if not all questions are answered.
                    var isIncomplete = !answers.Any() || questionnaire.Questions.Count != answers.Count();

                    return new DocumentSummaryItemDto
                    {
                        DocumentId = a.Id,
                        FileName = $"Add details for {a.FileName}",
                        FileSize = a.FileSize ?? 0,
                        BlobUri = a.DocumentStoragePath ?? string.Empty,
                        Status = (isIncomplete ? DocumentDetailStatus.Incomplete : DocumentDetailStatus.Completed).ToString(),
                    };
                });

            // Resolve all tasks and assign the resulting list of documents to the view model.
            viewModel.UploadedDocuments = [.. (await Task.WhenAll(tasks))];
        }

        // Render the view with the populated list of documents and their detail status.
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
                "Document details not found or an error occurred while retrieving them.");
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
            FileSize = documentDetailsResponse.Content.FileSize?.ToString() ?? string.Empty,
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
            var isValid = await ValidateQuestionnaire(documentDetail, validateMandatory: true);
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

        // If validation passes, proceed to the post-approval step.
        return RedirectToAction(
            "PostApproval",
            "ProjectOverview",
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
        // Retrieve contextual identifiers from TempData and HttpContext
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Fetch existing documents for this modification
        var response = await respondentService.GetModificationChangesDocuments(
            projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            projectRecordId,
            respondentId);

        if (model.Files is { Count: > 0 })
        {
            // Upload files to blob storage and get metadata for each file
            var uploadedBlobs = await blobStorageService.UploadFilesAsync(
                model.Files,
                ContainerName,
                model.IrasId.ToString());

            // Map uploaded blob metadata to DTOs for backend service
            var uploadedDocuments = uploadedBlobs.Select(uploadedBlob => new ProjectModificationDocumentRequest
            {
                ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
                ProjectRecordId = projectRecordId,
                ProjectPersonnelId = respondentId,
                FileName = uploadedBlob.FileName,
                DocumentStoragePath = uploadedBlob.BlobUri,
                FileSize = uploadedBlob.FileSize
            }).ToList();

            // Save the uploaded document metadata to the backend
            await projectModificationsService.CreateDocumentModification(uploadedDocuments);

            // Redirect to the review page
            return RedirectToAction(nameof(ModificationDocumentsAdded));
        }
        else if (response?.StatusCode != HttpStatusCode.OK)
        {
            // Show a service error page if backend fails
            return this.ServiceError(response!);
        }
        else if (response.Content != null && response.Content.Any())
        {
            // Documents already exist, redirect to review
            return RedirectToAction(nameof(ModificationDocumentsAdded));
        }
        else
        {
            // No existing docs and no new files uploaded
            ModelState.AddModelError("Files", "Please upload at least one document.");
            return View(model);
        }
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
    public async Task<IActionResult> SaveDocumentDetails(ModificationAddDocumentDetailsViewModel viewModel)
    {
        // Retrieve the CMS question set for the document metadata section
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build the full questionnaire from the CMS content
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Map user responses from the submitted view model to the questionnaire
        foreach (var question in questionnaire.Questions)
        {
            var response = viewModel.Questions.Find(q => q.Index == question.Index);

            question.SelectedOption = response?.SelectedOption;
            if (question.DataType != "Dropdown")
            {
                question.Answers = response?.Answers ?? [];
            }

            question.Id = response?.Id;
            question.AnswerText = response?.AnswerText;

            // Update date fields if present
            question.Day = response?.Day;
            question.Month = response?.Month;
            question.Year = response?.Year;
        }

        // Replace the original questions with the updated ones
        viewModel.Questions = questionnaire.Questions;

        // Validate the questionnaire and store the result in ViewData for UI messages
        var isValid = await ValidateQuestionnaire(viewModel);
        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        if (!isValid)
        {
            // Redisplay the view with validation errors
            return View("AddDocumentDetails", viewModel);
        }

        // Persist the responses to the backend service
        await SaveModificationDocumentAnswers(viewModel);

        // Redirect depending on whether the user is reviewing answers or adding more details
        return viewModel.ReviewAnswers
            ? RedirectToAction(nameof(ReviewDocumentDetails))
            : RedirectToAction(nameof(AddDocumentDetailsList));
    }

    /// <summary>
    /// Builds a <see cref="ProjectModificationDocumentRequest"/> using identifiers stored in TempData and HttpContext.
    /// </summary>
    /// <returns>The request object populated with modification change ID, project record ID, and respondent ID.</returns>
    private ProjectModificationDocumentRequest BuildDocumentRequest()
    {
        return new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = HttpContext.Items[ContextItemKeys.RespondentId] as string ?? string.Empty
        };
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
            documentChangeRequest.ProjectModificationChangeId,
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
                        Answers = cmsQ?.Answers?.Select(ans => new AnswerViewModel
                        {
                            AnswerId = ans.AnswerId,
                            AnswerText = ans.AnswerText,
                            IsSelected = ans.IsSelected
                        }).ToList() ?? new List<AnswerViewModel>(),
                        Rules = cmsQ?.Rules ?? new List<RuleDto>(),
                        ShortQuestionText = cmsQ?.ShortQuestionText ?? string.Empty,
                        IsModificationQuestion = true,
                        GuidanceComponents = cmsQ?.GuidanceComponents ?? new List<ContentComponent>()
                    };
                }).ToList()
            };

            viewModels.Add(vm);
        }

        return viewModels;
    }

    /// <summary>
    /// Validates a questionnaire using FluentValidation and populates ModelState with any errors.
    /// </summary>
    /// <param name="model">The questionnaire to validate.</param>
    /// <param name="validateMandatory">
    /// If true, only mandatory questions will be validated; otherwise, all questions are validated.
    /// </param>
    /// <returns>True if the questionnaire passes validation; false otherwise.</returns>
    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model, bool validateMandatory = false)
    {
        var context = new ValidationContext<QuestionnaireViewModel>(model);

        if (validateMandatory)
            context.RootContextData["ValidateMandatoryOnly"] = true;

        // Needed to access the questions within the validator
        context.RootContextData["questions"] = model.Questions;

        // Execute FluentValidation asynchronously
        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            // Populate ModelState with errors for display in the view
            foreach (var error in result.Errors)
            {
                if (error.CustomState is QuestionViewModel qvm)
                {
                    var adjustedPropertyName = PropertyNameHelper.AdjustPropertyName(error.PropertyName, qvm.Index);
                    ModelState.AddModelError(adjustedPropertyName, error.ErrorMessage);
                }
                else
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }

            return false;
        }

        return true;
    }
}