using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Extensions.Azure;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class TestDocumentsController : DocumentsController
{
    private readonly Queue<bool> _completionResults = new();

    public TestDocumentsController(
        IProjectModificationsService projectModificationsService,
        ICmsQuestionsetService cmsQuestionsetService,
        IRespondentService respondentService,
        IValidator<QuestionnaireViewModel> validator,
        IBlobStorageService blobStorageService,
        IAzureClientFactory<BlobServiceClient> blobServiceClientFactory,
        IFeatureManager featureManager)
        : base(projectModificationsService, respondentService, cmsQuestionsetService, validator, blobStorageService, blobServiceClientFactory, featureManager)
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