using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Controllers;

[Area("QuestionsManagement")]
[Route("[area]/[action]", Name = "qm:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
[FeatureGate(Features.Admin)]
public class HomeController(IQuestionSetService questionSetService) : Controller
{
    /// <summary>
    /// Displays the home page for the Questions Management area.
    /// </summary>
    [Route("/[area]", Name = "qm:home")]
    public async Task<IActionResult> Index(string versionId)
    {
        var catogoriesResponse = await questionSetService.GetQuestionCategories();
        var sectionsResponse = await questionSetService.GetQuestionSections();
        var questionsResponse = await questionSetService.GetQuestionsByVersion(versionId);

        if (!catogoriesResponse.IsSuccessStatusCode || !sectionsResponse.IsSuccessStatusCode || !questionsResponse.IsSuccessStatusCode)
        {
            return View("Error", this.ProblemResult(new()
            {
                ReasonPhrase = "Failed to load data",
                Error = "Unable to retrieve categories, sections, or questions.",
                StatusCode = catogoriesResponse.StatusCode
            }));
        }

        var categories = catogoriesResponse.Content;
        var sections = sectionsResponse.Content;
        var questions = questionsResponse.Content;

        var model = new QuestionsViewModel
        {
            Categories = categories!
                            .Where(c => c.VersionId == versionId)
                            .Adapt<List<QuestionCategoryViewModel>>(),

            Sections = sections!
                            .Where(s => s.VersionId == versionId)
                            .Adapt<List<QuestionSectionViewModel>>(),

            Questions = questions!.Adapt<List<QuestionViewModel>>()
        };

        return View(model);
    }

    /// <summary>
    /// Manages the rules for questions in a specific version.
    /// </summary>
    public async Task<IActionResult> ManageRules(string questionId, string versionId)
    {
        var questionsResponse = await questionSetService.GetQuestionsByVersion(versionId);

        if (!questionsResponse.IsSuccessStatusCode)
        {
            return View("Error", this.ProblemResult(new()
            {
                ReasonPhrase = "Failed to load questions",
                Error = "Unable to retrieve questions for the specified version.",
                StatusCode = questionsResponse.StatusCode
            }));
        }

        var questions = questionsResponse.Content;
        var model = new RuleViewModel
        {
            QuestionId = questionId,
            QuestionText = questions!.FirstOrDefault(q => q.QuestionId == questionId)?.QuestionText ?? string.Empty,
            ParentQuestions = [.. questions!
                .Where(q => q.QuestionId != questionId)
                .Select(q => new SelectListItem
                {
                    Value = q.QuestionId,
                    Text = q.QuestionText
                })]
        };

        return View("Rules", model);
    }
}