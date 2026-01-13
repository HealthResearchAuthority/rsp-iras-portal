using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Extensions.Azure;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class TestDocumentsController : DocumentsController
{
    private readonly Queue<bool> _completionResults = new();

    public TestDocumentsController(
        IProjectModificationsService projectModificationsService,
        ICmsQuestionsetService cmsQuestionsetService,
        IRespondentService respondentService,
        IValidator<QuestionnaireViewModel> validator,
        IBlobStorageService blobStorageService,
        IAzureClientFactory<BlobServiceClient> blobServiceClientFactory)
        : base(projectModificationsService, respondentService, cmsQuestionsetService, validator, blobStorageService, blobServiceClientFactory)
    {
    }

    public void SetEvaluateDocumentCompletionResults(params bool[] results)
    {
        foreach (var r in results)
        {
            _completionResults.Enqueue(r);
        }
    }

    protected override Task<bool> EvaluateDocumentCompletion(Guid documentId, QuestionnaireViewModel questionnaire)
    {
        return Task.FromResult(_completionResults.Dequeue());
    }
}