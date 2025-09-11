using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Components;

public class BackNavigation(ICmsQuestionsetService cmsQuestionsetService) : ViewComponent
{
    private NavigationDto _navigationDto = null!;
    private string _specificAreaOfChangeId = null!;
    private string _projectRecordId = null!;
    private QuestionnaireViewModel? _questionnaire;

    private const string BackButtonText = "Back";
    private const string ReviewPageRoute = "pmc:reviewchanges";
    private const string AreaOfChangePageRoute = "pmc:areaofchange";

    public async Task<IViewComponentResult> InvokeAsync
    (
        NavigationDto navigationDto,
        string specificAreaOfChangeId,
        string projectRecordId,
        QuestionnaireViewModel? questionnaire = null,
        bool backFromReview = false,
        bool reviewInProgress = false
    )
    {
        _navigationDto = navigationDto;
        _specificAreaOfChangeId = specificAreaOfChangeId;
        _projectRecordId = projectRecordId;
        _questionnaire = questionnaire;

        var navigationModel = (backFromReview, reviewInProgress) switch
        {
            (false, true) => ResolveBackNavigationForReview(),
            (true, false) => await ResolveBackNavigationFromReview(cmsQuestionsetService),
            _ => ResolveBackNavigation()
        };

        return View("BackNavigation", navigationModel);
    }

    private (string RouteName, string Text, Dictionary<string, string> parameters) ResolveBackNavigation()
    {
        var previousSection = _navigationDto?.PreviousSection;

        var previousRoute = previousSection == null ?
            AreaOfChangePageRoute :
            $"pmc:{previousSection.StaticViewName}";

        var parameters = previousSection == null ?
            new Dictionary<string, string>() :
            new()
            {
                { "projectRecordId", _projectRecordId },
                { "categoryId", previousSection.QuestionCategoryId },
                { "sectionId", previousSection.SectionId! }
            };

        return (previousRoute, BackButtonText, parameters);
    }

    /// <summary>
    /// Resolves the back navigation when ReviewInProgress is true, e.g. a link is clicked to make a change
    /// In this case clicking on the back button should go back to the review page
    /// </summary>
    private (string RouteName, string Text, Dictionary<string, string> parameters) ResolveBackNavigationForReview()
    {
        var parameters = new Dictionary<string, string>
        {
            { "projectRecordId", _projectRecordId }
        };

        return (ReviewPageRoute, BackButtonText, parameters);
    }

    /// <summary>
    /// Resolves the back navigation when clicking on the back button from review page
    /// We need to figure out the correct previous section as it could be dependent on the
    /// section prior to it, and if answers are missing, then the parent question section should be shown
    /// </summary>
    private async Task<(string RouteName, string Text, Dictionary<string, string> parameters)> ResolveBackNavigationFromReview(ICmsQuestionsetService cmsQuestionsetService)
    {
        // get questions from CMS
        var modificationJourney = await cmsQuestionsetService.GetModificationsJourney(_specificAreaOfChangeId);

        var response = modificationJourney.Content!;

        var lastSectionId = response
            .Sections.OrderByDescending(section => section.Sequence)
            .First().Id;

        var questions = _questionnaire.Questions;

        var previousRoute = string.Empty;

        Dictionary<string, string> parameters = [];

        // loop through each section, and check if the section matches the lastSectionId
        // or if section is mandatory, and answers are missing, that would be the section where back button should go from the review
        // page.
        foreach (var section in response.Sections)
        {
            parameters = new()
            {
                { "projectRecordId", _projectRecordId },
                { "categoryId", section.CategoryId },
                { "sectionId", section.Id }
            };

            previousRoute = $"pmc:{section.StaticViewName}";

            if (section.Id == lastSectionId)
            {
                break;
            }

            if (section.IsMandatory)
            {
                var answers = questions.FindAll(question => question.SectionId == section.Id && !question.IsMissingAnswer());

                if (answers.Count == 0)
                {
                    break;
                }
            }
        }

        return (previousRoute, BackButtonText, parameters);
    }
}