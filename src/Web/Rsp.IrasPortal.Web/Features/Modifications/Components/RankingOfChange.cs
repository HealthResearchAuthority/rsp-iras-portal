using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Helpers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Components;

public class RankingOfChange(ICmsQuestionsetService cmsQuestionsetService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync
    (
        string specificAreaOfChangeId,
        bool applicability,
        IEnumerable<QuestionViewModel> questions
    )
    {
        var rankingOfChangeRequest = ModificationHelpers.GetRankingOfChangeRequest(specificAreaOfChangeId, applicability, questions);

        var ranking = await cmsQuestionsetService.GetModificationRanking(rankingOfChangeRequest);

        var rankingOfChangeViewModel = new RankingOfChangeViewModel
        {
            ModificationType = ranking?.Content?.ModificationType.Substantiality ?? "Not available",
            Category = ranking?.Content?.Categorisation.Category ?? "Not available",
            ReviewType = ranking?.Content?.ReviewType ?? "Not available"
        };

        return View("/Features/Modifications/Shared/RankingOfChange.cshtml", rankingOfChangeViewModel);
    }
}