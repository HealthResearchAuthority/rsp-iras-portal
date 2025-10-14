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

            Dictionary<string, string> answerOptions = new()
            {
                { QuestionAnswersOptionsIds.Yes, "Yes" },
                { QuestionAnswersOptionsIds.No, "No" }
            };

            var answers = respondentAnswersResponse.Content;
            nhsOrHscOrganisations = GetAnswerName(answers.FirstOrDefault(a => a.QuestionId == QuestionIds.NhsOrHscOrganisations)?.SelectedOption, answerOptions);
        }

        var rankingOfChangeRequest = ModificationHelpers.GetRankingOfChangeRequest(specificAreaOfChangeId, applicability, questions, nhsOrHscOrganisations);

        var ranking = await cmsQuestionsetService.GetModificationRanking(rankingOfChangeRequest);

        var rankingOfChangeViewModel = new RankingOfChangeViewModel
        {
            ModificationType = ranking?.Content?.ModificationType.Substantiality ?? "Not available",
            Category = ranking?.Content?.Categorisation.Category ?? "N/A",
            ReviewType = ranking?.Content?.ReviewType ?? "Not available"
        };

        return View("/Features/Modifications/Shared/RankingOfChange.cshtml", rankingOfChangeViewModel);
    }

    private static string? GetAnswerName(string? answerText, Dictionary<string, string> options)
    {
        return answerText is string id && options.TryGetValue(id, out var name) ? name : null;
    }
}