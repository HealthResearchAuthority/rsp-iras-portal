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
    public async Task<IActionResult> Index(QuestionsViewModel model)
    {
        var catogoriesResponse = await questionSetService.GetQuestionCategories();
        var sectionsResponse = await questionSetService.GetQuestionSections();
        var questionsResponse = await questionSetService.GetQuestionsByVersion(model.VersionId);

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
        var questions = questionsResponse!.Content!.AsQueryable();

        model.QuestionTypes = [..questions
            .DistinctBy(question => question.DataType)
                .Select(question => question.DataType)];

        model.Categories = categories!
                            .Where(c => c.VersionId == model.VersionId)
                            .Adapt<List<QuestionCategoryViewModel>>();

        model.Sections = sections!
                            .Where(s => s.VersionId == model.VersionId)
                            .Adapt<List<QuestionSectionViewModel>>();

        if (!string.IsNullOrWhiteSpace(model.SelectedCategory))
            questions = questions.Where(q => q.Category == model.SelectedCategory);

        if (!string.IsNullOrWhiteSpace(model.SelectedSection))
            questions = questions.Where(q => q.SectionId == model.SelectedSection);

        if (!string.IsNullOrWhiteSpace(model.SelectedType))
            questions = questions.Where(q => q.DataType == model.SelectedType);

        model.Questions = questions!.ToList().Adapt<List<QuestionViewModel>>();

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

    [Route("[area]/categories", Name = "qm:categories")]
    public async Task<IActionResult> Categories(string versionId)
    {
        var categoriesResponse = await questionSetService.GetQuestionCategories();

        if (!categoriesResponse.IsSuccessStatusCode)
        {
            return View("Error", this.ProblemResult(new()
            {
                ReasonPhrase = "Failed to load categories",
                Error = "Unable to retrieve question categories.",
                StatusCode = categoriesResponse.StatusCode
            }));
        }

        var categories = categoriesResponse.Content!
            .Where(c => c.VersionId == versionId)
            .Adapt<List<QuestionCategoryViewModel>>();
        return View(categories);
    }

    [Route("[area]/sections", Name = "qm:sections")]
    public async Task<IActionResult> Sections(string versionId)
    {
        var sectionsResponse = await questionSetService.GetQuestionSections();

        if (!sectionsResponse.IsSuccessStatusCode)
        {
            return View("Error", this.ProblemResult(new()
            {
                ReasonPhrase = "Failed to load sections",
                Error = "Unable to retrieve question sections.",
                StatusCode = sectionsResponse.StatusCode
            }));
        }

        var categories = sectionsResponse.Content!
            .Where(c => c.VersionId == versionId)
            .Adapt<List<QuestionSectionViewModel>>();
        return View(categories);
    }
}