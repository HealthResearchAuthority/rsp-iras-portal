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
[Route("[area]/[controller]/[action]", Name = "qrm:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
[FeatureGate(Features.Admin)]
public class RulesController(IQuestionSetService questionSetService) : Controller
{
    ///// <summary>
    ///// Displays the home page for the Questions Management area.
    ///// </summary>
    //public async Task<IActionResult> Index(QuestionsViewModel model)
    //{
    //    var catogoriesResponse = await questionSetService.GetQuestionCategories();
    //    var sectionsResponse = await questionSetService.GetQuestionSections();
    //    var questionsResponse = await questionSetService.GetQuestionsByVersion(model.VersionId);

    //    if (!catogoriesResponse.IsSuccessStatusCode || !sectionsResponse.IsSuccessStatusCode || !questionsResponse.IsSuccessStatusCode)
    //    {
    //        return View("Error", this.ProblemResult(new()
    //        {
    //            ReasonPhrase = "Failed to load data",
    //            Error = "Unable to retrieve categories, sections, or questions.",
    //            StatusCode = catogoriesResponse.StatusCode
    //        }));
    //    }

    //    var categories = catogoriesResponse.Content;
    //    var sections = sectionsResponse.Content;
    //    var questions = questionsResponse!.Content!.AsQueryable();

    //    model.QuestionTypes = [..questions
    //        .DistinctBy(question => question.DataType)
    //            .Select(question => question.DataType)];

    //    model.Categories = categories!
    //                        .Where(c => c.VersionId == model.VersionId)
    //                        .Adapt<List<QuestionCategoryViewModel>>();

    //    model.Sections = sections!
    //                        .Where(s => s.VersionId == model.VersionId)
    //                        .Adapt<List<QuestionSectionViewModel>>();

    //    if (!string.IsNullOrWhiteSpace(model.SelectedCategory))
    //        questions = questions.Where(q => q.Category == model.SelectedCategory);

    //    if (!string.IsNullOrWhiteSpace(model.SelectedSection))
    //        questions = questions.Where(q => q.SectionId == model.SelectedSection);

    //    if (!string.IsNullOrWhiteSpace(model.SelectedType))
    //        questions = questions.Where(q => q.DataType == model.SelectedType);

    //    model.Questions = questions!.ToList().Adapt<List<QuestionViewModel>>();

    //    return View(model);
    //}

    /// <summary>
    /// Manages the rules for questions in a specific version.
    /// </summary>
    public async Task<IActionResult> AddRule(string questionId, string versionId)
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

        var questions = questionsResponse.Content!;

        var question = questions.Single(q => q.QuestionId == questionId);

        var ruleViewModel = new RuleViewModel
        {
            QuestionId = questionId,
            QuestionText = question.QuestionText ?? string.Empty,
            ParentQuestions = [.. questions!
                .Where(q => q.QuestionId != questionId && q.DataType.ToLower() is "checkbox" or "radio button" or "boolean")
                .Select(q => new SelectListItem
                {
                    Value = q.QuestionId,
                    Text = q.QuestionText
                })]
        };

        return View("AddRule", ruleViewModel);
    }
}