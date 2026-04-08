using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.Components;

public class ParticipatingOrganisationsReview
(
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService,
    IRtsService rtsService
) : ViewComponent
{
    /// <summary>
    /// The CMS section identifier used to retrieve the participating organisation details question set.
    /// </summary>
    private const string OrganisationDetailsSection = "pom-participating-organisation-details";

    private Guid _modificationChangeId;

    private string _projectRecordId = null!;

    public async Task<IViewComponentResult> InvokeAsync
    (
        string projectRecordId,
        string modificationChangeId,
        string specificAreaOfChangeId,
        bool showLinks = true
    )
    {
        _projectRecordId = projectRecordId;
        _modificationChangeId = Guid.Parse(modificationChangeId);

        var organisationsWithDetails = specificAreaOfChangeId == SpecificAreasOfChange.AddNewSites ?
            await GetOrganisationsWithDetails() :
            await GetOrganisations();

        ViewData[ViewDataKeys.ShowParticipatingOrgsLinks] = showLinks;

        return View("/Features/Modifications/ParticipatingOrganisations/Components/ParticipatingOrganisationsReview.cshtml", organisationsWithDetails);

        // Validate the questionnaire; details are incomplete if there are no answers or validation fails
        // var isValid = await this.ValidateQuestionnaire(validator, questionnaire, true, false);
        //return View();
    }

    private async Task<IList<OrganisationDetailsViewModel>> GetOrganisations()
    {
        var context = ViewContext;

        // Fetch all organisations for the change
        var response = await respondentService.GetModificationParticipatingOrganisations
        (
            _modificationChangeId,
            _projectRecordId
        );

        var viewModels = new List<OrganisationDetailsViewModel>();

        foreach (var org in response!.Content!)
        {
            var organisationResponse = await rtsService.GetOrganisation(org.OrganisationId);

            var organisation = organisationResponse?.StatusCode == HttpStatusCode.OK ?
                organisationResponse.Content :
                null;

            if (organisation == null)
            {
                continue; // Skip if organisation details cannot be retrieved
            }

            // Map orgs and questions into a view model
            var vm = new OrganisationDetailsViewModel
            {
                Id = org.Id,
                OrganisationId = org.OrganisationId,
                OrganisationName = organisation.Name,
                OrganisationAddress = organisation.Address,
                OrganisationCountryName = organisation.CountryName,
                OrganisationType = organisation.Type,
                ReviewAnswers = true
            };

            viewModels.Add(vm);
        }

        return viewModels;
    }

    /// <summary>
    /// Retrieves all uploaded modification documents with their respective answers.
    /// Constructs a view model for displaying the documents and questions.
    /// </summary>
    /// <returns>A list of <see cref="OrganisationDetailsViewModel"/> with answers populated.</returns>
    private async Task<IList<OrganisationDetailsViewModel>> GetOrganisationsWithDetails()
    {
        var reviseChange = ViewContext.HttpContext.Request.Query["reviseChange"].ToString() is "true" or "True";
        var reviewAllChanges = TempData.Peek(TempDataKeys.ProjectModification.ReviewAllChanges) is true;

        // Fetch all organisations for the change
        var response = await respondentService.GetModificationParticipatingOrganisations
        (
            _modificationChangeId,
            _projectRecordId
        );

        // Retrieve the CMS question set for the organisation details section
        var additionalQuestionsResponse = await cmsQuestionsetService.GetModificationQuestionSet(OrganisationDetailsSection);

        var viewModels = new List<OrganisationDetailsViewModel>();

        var questionIndex = 0;

        foreach (var org in response!.Content!)
        {
            var organisationResponse = await rtsService.GetOrganisation(org.OrganisationId);

            var organisation = organisationResponse?.StatusCode == HttpStatusCode.OK ?
                organisationResponse.Content :
                null;

            if (organisation == null)
            {
                continue; // Skip if organisation details cannot be retrieved
            }

            // Fetch existing answers for this organisation
            var answersResponse = await respondentService.GetModificationParticipatingOrganisationAnswers(org.Id);
            var answers = answersResponse?.StatusCode == HttpStatusCode.OK
                ? answersResponse.Content ?? [] : [];

            var cmsQuestions = QuestionsetHelpers.BuildQuestionnaireViewModel(additionalQuestionsResponse.Content!);

            // Map orgs and questions into a view model
            var vm = new OrganisationDetailsViewModel
            {
                Id = org.Id,
                OrganisationId = org.OrganisationId,
                OrganisationName = organisation.Name,
                OrganisationAddress = organisation.Address,
                OrganisationCountryName = organisation.CountryName,
                OrganisationType = organisation.Type,
                ReviewAnswers = true,

                Questions = cmsQuestions.Questions.ConvertAll(cmsQ =>
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
                        .ToList() ?? [],
                        Rules = cmsQ?.Rules ?? [],
                        ShortQuestionText = cmsQ?.ShortQuestionText ?? string.Empty,
                        IsModificationQuestion = true,
                        GuidanceComponents = cmsQ?.GuidanceComponents ?? []
                    };
                })
            };

            viewModels.Add(vm);
        }

        return viewModels;
    }
}