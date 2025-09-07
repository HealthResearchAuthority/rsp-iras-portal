using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Domain.Enums;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController : Controller
{
    private const string ContainerName = "staging";
    private const string DocumentDetailsSection = "pdm-document-metadata";

    /// <summary>
    /// Returns the view for uploading project documents.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ProjectDocument()
    {
        var specificAreaOfChange = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string;

        var viewModel = TempData.PopulateBaseProjectModificationProperties(new ModificationUploadDocumentsViewModel());

        viewModel.PageTitle = !string.IsNullOrEmpty(specificAreaOfChange) ?
            $"Add documents for {specificAreaOfChange.ToLowerInvariant()}"
            : string.Empty;

        return View(nameof(UploadDocuments), viewModel);
    }

    /// <summary>
    /// Displays the review page for uploaded project modification documents.
    /// Fetches document metadata from the backend service.
    /// </summary>
    /// <returns>The review view with the list of uploaded documents or an error message if none found.</returns>
    [HttpGet]
    public async Task<IActionResult> ModificationDocumentsAdded()
    {
        // Fetch contextual data for the view
        var specificAreaOfChange = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationReviewDocumentsViewModel
        {
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Documents added for {specificAreaOfChange.ToLowerInvariant()}"
                : string.Empty
        };

        // Create request for fetching documents
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId)!,
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
            viewModel.UploadedDocuments = response.Content
                .Select(a => new DocumentSummaryItemDto
                {
                    FileName = a.FileName,
                    FileSize = a.FileSize ?? 0,
                    BlobUri = a.DocumentStoragePath
                })
                .OrderBy(dto => dto.FileName, StringComparer.OrdinalIgnoreCase) // sort alphabetically
                .ToList();
        }
        else
        {
            // Handle the case where no documents were returned or service failed
            viewModel.UploadedDocuments = [];
            ModelState.AddModelError(string.Empty, "No documents found or an error occurred while retrieving documents.");
        }

        return View(nameof(ModificationDocumentsAdded), viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> AddDocumentDetailsList()
    {
        // Fetch contextual data for the view
        var specificAreaOfChange = TempData.Peek(TempDataKeys.ProjectModification.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationReviewDocumentsViewModel
        {
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Add document details for {specificAreaOfChange.ToLowerInvariant()}"
                : string.Empty
        };

        // Create request for fetching documents
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Get CMS question set
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build questionnaire (all questions)
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Call the respondent service to retrieve uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            var tasks = response.Content
            .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(async a =>
            {
                var answersResponse = await respondentService.GetModificationDocumentAnswers(a.Id);
                var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                    ? answersResponse.Content ?? new List<ProjectModificationDocumentAnswerDto>()
                    : new List<ProjectModificationDocumentAnswerDto>();

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

            viewModel.UploadedDocuments = (await Task.WhenAll(tasks)).ToList();
        }

        return View(nameof(AddDocumentDetailsList), viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ContinueToDetails(Guid documentId, bool reviewAnswers = false)
    {
        var documentDetailsResponse = await respondentService.GetModificationDocumentDetails(documentId);
        if (documentDetailsResponse?.StatusCode != HttpStatusCode.OK || documentDetailsResponse.Content == null)
        {
            ModelState.AddModelError(string.Empty, "Document details not found or an error occurred while retrieving them.");
            return RedirectToAction(nameof(AddDocumentDetailsList));
        }

        // Populate base document details
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

        // Get CMS question set
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build questionnaire (all questions)
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // Get existing answers
        var answersResponse = await respondentService.GetModificationDocumentAnswers(documentId);
        var answers = answersResponse?.StatusCode == HttpStatusCode.OK
        ? answersResponse.Content ?? new List<ProjectModificationDocumentAnswerDto>()
        : new List<ProjectModificationDocumentAnswerDto>();

        if (answers.Any())
        {
            foreach (var ans in answers)
            {
                // Find the matching question in questionnaire and update with answer
                var matchingQuestion = questionnaire.Questions.FirstOrDefault(q => q.QuestionId == ans.QuestionId);
                if (matchingQuestion != null)
                {
                    matchingQuestion.Id = ans.Id;
                    matchingQuestion.AnswerText = ans.AnswerText;
                    matchingQuestion.SelectedOption = ans.SelectedOption;
                }
            }
        }

        viewModel.Questions = questionnaire.Questions;

        return View("AddDocumentDetails", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewDocumentDetails()
    {
        var viewModels = await GetAllDocumentsWithResponses();

        return View("ReviewDocumentDetails", viewModels);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewAllDocumentDetails()
    {
        var allDocumentDetails = await GetAllDocumentsWithResponses();
        var hasErrors = false;

        foreach (var documentDetail in allDocumentDetails)
        {
            var isValid = await ValidateQuestionnaire(documentDetail, true);

            if (!isValid)
            {
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            // Return the view with the invalid models + ModelState errors
            return View("ReviewDocumentDetails", allDocumentDetails);
        }

        // If all pass, proceed with your next step
        return RedirectToAction("PostApproval", "ProjectOverview", new { projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty });
    }

    [HttpPost]
    public IActionResult AddAnotherDocument()
    {
        return RedirectToAction(nameof(UploadDocuments));
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
        // Retrieve contextual identifiers from TempData and HttpContext
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId);
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Call the respondent service to retrieve uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
            projectRecordId,
            respondentId);

        if (model.Files is { Count: > 0 })
        {
            // Upload files to blob storage and return metadata
            var uploadedBlobs = await blobStorageService.UploadFilesAsync(
                model.Files,
                ContainerName,
                model.IrasId.ToString());

            // Map uploaded blob metadata to document request DTOs
            var uploadedDocuments = new List<ProjectModificationDocumentRequest>();
            foreach (var uploadedBlob in uploadedBlobs)
            {
                var document = new ProjectModificationDocumentRequest
                {
                    ProjectModificationChangeId = projectModificationChangeId == null
                        ? Guid.Empty
                        : (Guid)projectModificationChangeId!,
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

            return RedirectToAction(nameof(ModificationDocumentsAdded));
        }
        else if (response?.StatusCode != HttpStatusCode.OK)
        {
            // Show a service error page
            return this.ServiceError(response!);
        }
        else if (response.Content != null && response.Content.Any())
        {
            // Documents already exist, go to review
            return RedirectToAction(nameof(ModificationDocumentsAdded));
        }
        else
        {
            // No existing docs and no new files uploaded
            ModelState.AddModelError("Files", "Please upload at least one document.");
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveDocumentDetails(ModificationAddDocumentDetailsViewModel viewModel)
    {
        // Get CMS question set
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);

        // Build questionnaire (all questions)
        var questionnaire = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        // update the model with the answeres
        // provided by the applicant
        foreach (var question in questionnaire.Questions)
        {
            // find the question in the submitted model
            // that matches the index
            var response = viewModel.Questions.Find(q => q.Index == question.Index);

            // update the question with provided answers
            question.SelectedOption = response?.SelectedOption;
            if (question.DataType != "Dropdown")
            {
                question.Answers = response?.Answers ?? [];
            }

            question.Id = response?.Id;
            question.AnswerText = response?.AnswerText;
            // update the date fields if they are present
            question.Day = response?.Day;
            question.Month = response?.Month;
            question.Year = response?.Year;
        }

        viewModel.Questions = questionnaire.Questions;

        // validate the questionnaire and save the result in tempdata
        // this is so we display the validation passed message or not
        var isValid = await ValidateQuestionnaire(viewModel);
        ViewData[ViewDataKeys.IsQuestionnaireValid] = isValid;

        if (!isValid)
        {
            return View("AddDocumentDetails", viewModel);
        }

        await SaveModificationDocumentAnswers(viewModel);

        if (viewModel.ReviewAnswers)
        {
            return RedirectToAction(nameof(ReviewDocumentDetails));
        }

        return RedirectToAction(nameof(AddDocumentDetailsList));
    }

    private async Task SaveModificationDocumentAnswers(ModificationAddDocumentDetailsViewModel viewModel)
    {
        // to save the responses
        // we need to build the ProjectModificationDocumentAnswerDto
        // populate the RespondentAnswers
        var request = new List<ProjectModificationDocumentAnswerDto>();

        foreach (var question in viewModel.Questions)
        {
            // we need to identify if it's a
            // multiple choice or a single choice question
            // this is to determine if the responses
            // should be saved as comma seprated values
            // or a single value
            var optionType = question.DataType switch
            {
                "Boolean" or "Radio button" or "Look-up list" or "Dropdown" => "Single",
                "Checkbox" => "Multiple",
                _ => null
            };

            // build RespondentAnswers model
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

        // if user has answered some or all of the questions
        // call the api to save the responses
        if (request.Count > 0)
        {
            await respondentService.SaveModificationDocumentAnswers(request);
        }
    }

    private async Task<IList<ModificationAddDocumentDetailsViewModel>> GetAllDocumentsWithResponses()
    {
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModification.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Get uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        // Get CMS question set (all possible questions for a document)
        var additionalQuestionsResponse = await cmsQuestionsetService
            .GetModificationQuestionSet(DocumentDetailsSection);
        var cmsQuestions = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

        var viewModels = new List<ModificationAddDocumentDetailsViewModel>();

        foreach (var doc in response!.Content.OrderBy(d => d.FileName, StringComparer.OrdinalIgnoreCase))
        {
            // Get answers for this document (may be empty or partial)
            var answersResponse = await respondentService.GetModificationDocumentAnswers(doc.Id);
            var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                ? answersResponse.Content ?? new List<ProjectModificationDocumentAnswerDto>()
                : new List<ProjectModificationDocumentAnswerDto>();

            // Build VM
            var vm = new ModificationAddDocumentDetailsViewModel
            {
                DocumentId = doc.Id,
                FileName = doc.FileName,
                DocumentStoragePath = doc.DocumentStoragePath,
                ReviewAnswers = true,
                Questions = cmsQuestions.Questions.Select((cmsQ, index) =>
                {
                    // Find the answer for this CMS question (if any)
                    var matchingAnswer = answers.FirstOrDefault(a => a.QuestionId == cmsQ.QuestionId);

                    return new QuestionViewModel
                    {
                        Id = matchingAnswer?.Id,
                        Index = index,
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
                        }).ToList() ?? [],
                        Rules = cmsQ?.Rules ?? [],
                        ShortQuestionText = cmsQ?.ShortQuestionText ?? string.Empty,
                        IsModificationQuestion = true,
                        GuidanceComponents = cmsQ?.GuidanceComponents ?? []
                    };
                }).ToList()
            };

            viewModels.Add(vm);
        }

        return viewModels;
    }

    /// <summary>
    /// Validates the passed QuestionnaireViewModel and return ture or false
    /// </summary>
    /// <param name="model"><see cref="QuestionnaireViewModel"/> to validate</param>
    private async Task<bool> ValidateQuestionnaire(QuestionnaireViewModel model, bool validateMandatory = false)
    {
        // using the FluentValidation, create a new context for the model
        var context = new ValidationContext<QuestionnaireViewModel>(model);

        if (validateMandatory)
        {
            context.RootContextData["ValidateMandatoryOnly"] = true;
        }

        // this is required to get the questions in the validator
        // before the validation cicks in
        context.RootContextData["questions"] = model.Questions;

        // call the ValidateAsync to execute the validation
        // this will trigger the fluentvalidation using the injected validator if configured
        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            // Copy the validation results into ModelState.
            // ASP.NET uses the ModelState collection to populate
            // error messages in the View.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return false;
        }

        return true;
    }
}