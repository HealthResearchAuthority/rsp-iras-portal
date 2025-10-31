using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Components;

public class RankingOfChange(ICmsQuestionsetService cmsQuestionsetService,
    IRespondentService respondentService) : ViewComponent
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
                await respondentService.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecrod);

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

        var rankingOfChangeViewModel = new RankingOfChangeViewModel
        {
            ModificationType = ranking?.Content?.ModificationType.Substantiality ?? Ranking.NotAvailable,
            Category = ranking?.Content?.Categorisation.Category ?? Ranking.NotAvailable,
            ReviewType = ranking?.Content?.ReviewType ?? Ranking.NotAvailable
        };

        return View("/Features/Modifications/Shared/RankingOfChange.cshtml", rankingOfChangeViewModel);
    }
}