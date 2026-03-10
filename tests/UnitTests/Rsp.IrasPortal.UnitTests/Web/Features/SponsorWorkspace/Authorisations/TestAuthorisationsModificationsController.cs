using FluentValidation;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class TestAuthorisationsModificationsController : AuthorisationsModificationsController
{
    private bool? _forcedCompletionResult;

    public TestAuthorisationsModificationsController(
        IProjectModificationsService projectModificationsService,
        IRespondentService respondentService,
        ISponsorOrganisationService sponsorOrganisationService,
        ICmsQuestionsetService cmsQuestionsetService,
        ISponsorUserAuthorisationService sponsorUserAuthorisationService,
        IValidator<AuthorisationsModificationsSearchModel> searchValidator,
        IValidator<AuthoriseModificationsOutcomeViewModel> outcomeValidator,
        IValidator<QuestionnaireViewModel> questionnaireValidator,
        IFeatureManager featureManager,
        IRtsService rtsService,
        IApplicationsService applicationsService
    )
        : base(
            projectModificationsService,
            respondentService,
            sponsorOrganisationService,
            cmsQuestionsetService,
            sponsorUserAuthorisationService,
            searchValidator,
            outcomeValidator,
            questionnaireValidator,
            featureManager,
            rtsService,
            applicationsService
        )
    {
    }

    public void SetEvaluateDocumentCompletionResult(bool result)
    {
        _forcedCompletionResult = result;
    }

    protected override Task<bool> EvaluateDocumentCompletion(
        Guid documentId,
        QuestionnaireViewModel questionnaire,
        bool addModelErrors = true)
    {
        // always return the forced result, no matter how often it's called
        return Task.FromResult(_forcedCompletionResult ?? false);
    }
}