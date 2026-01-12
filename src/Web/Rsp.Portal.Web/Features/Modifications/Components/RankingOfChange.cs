using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Helpers;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Features.Modifications.Components;

public class RankingOfChange
(
    ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService
) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync
    (
        string projectRecordId,
        string specificAreaOfChangeId,
        bool applicability,
        IEnumerable<QuestionViewModel> questions
    )
    {
        // if applicability is false, call project record look up for IQA0004,
        string nhsOrHscOrganisations = string.Empty;

        if (!applicability)
        {
            // Get all respondent answers for the project and category
            var respondentAnswersResponse =
                await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecord);

            var answers = respondentAnswersResponse.Content;

            var answer = answers?.FirstOrDefault(a => a.QuestionId == QuestionIds.NhsOrHscOrganisations);

            nhsOrHscOrganisations = answer?.SelectedOption switch
            {
                var option when option is QuestionAnswersOptionsIds.Yes => "Yes",
                _ => "No"
            };
        }

        var rankingOfChangeRequest = ModificationHelpers.GetRankingOfChangeRequest(specificAreaOfChangeId, applicability, questions, nhsOrHscOrganisations);

        var ranking = await cmsQuestionsetService.GetModificationRanking(rankingOfChangeRequest);

        var modificationType = ranking?.Content?.ModificationType?.Substantiality ?? Ranking.NotAvailable;

        // If modification type is Non-Notifiable, force category to N/A
        var category = string.Equals(modificationType, Ranking.ModificationTypes.NonNotifiable, StringComparison.OrdinalIgnoreCase)
            ? Ranking.CategoryTypes.NA
            : ranking?.Content?.Categorisation?.Category ?? Ranking.NotAvailable;

        var rankingOfChangeViewModel = new RankingOfChangeViewModel
        {
            ModificationType = modificationType,
            Category = category,
            ReviewType = ranking?.Content?.ReviewType ?? Ranking.NotAvailable
        };

        // store the ranking in TempData to be saved in database on confirmation of change
        // in review changes page
        TempData[TempDataKeys.ProjectModificationChange.RankingOfChange] = JsonSerializer.Serialize(rankingOfChangeViewModel);

        return View("/Features/Modifications/Shared/RankingOfChange.cshtml", rankingOfChangeViewModel);
    }
}