using System.Globalization;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel representing a questionnaire, including its questions and related state.
/// </summary>
public class QuestionnaireViewModel
{
    /// <summary>
    /// Indicates if the answers are being reviewed.
    /// </summary>
    public bool ReviewAnswers { get; set; }

    /// <summary>
    /// The current stage of the questionnaire.
    /// </summary>
    public string? CurrentStage { get; set; } = "";

    /// <summary>
    /// List of questions in the questionnaire.
    /// </summary>
    public List<QuestionViewModel> Questions { get; set; } = [];

    public List<ContentComponent> GuidanceContent { get; set; } = [];

    public List<ContentComponent> GuidanceContent { get; set; } = [];

    /// <summary>
    /// ViewModel for searching and selecting a sponsor organisation.
    /// </summary>
    public OrganisationSearchViewModel SponsorOrgSearch { get; set; } = new();

    /// <summary>
    /// Gets a list of conditional rules for non-mandatory questions that have rules.
    /// </summary>
    /// <returns>List of objects containing QuestionId and Rules for applicable questions.</returns>
    public List<object> GetConditionalRules()
    {
        return [.. Questions
            .Where(q => !q.IsMandatory && q.Rules.Any())
            .Select(q => new
            {
                q.QuestionId,
                q.Rules
            })];
    }

    /// <summary>
    /// Gets the answer text for the question with text "Short project title", if present.
    /// </summary>
    /// <returns>The answer text for the short project title, or null if not found.</returns>
    public string? GetShortProjectTitle()
    {
        return Questions
            .FirstOrDefault(q => q.QuestionId.Equals(QuestionIds.ShortProjectTitle, StringComparison.OrdinalIgnoreCase))?.AnswerText;
    }

    /// <summary>
    /// Gets the answer text for the question with QuestionId "IQA0003", which represents the planned end date of the project.
    /// </summary>
    /// <returns>
    /// The answer text for the planned end date of the project, or null if the question is not found.
    /// </returns>
    public string? GetProjectPlannedEndDate()
    {
        var ukCulture = new CultureInfo("en-GB");

        var plannedEndDate = Questions.FirstOrDefault(q => q.QuestionId.Equals(QuestionIds.ProjectPlannedEndDate, StringComparison.OrdinalIgnoreCase))?.AnswerText;

        if (DateTime.TryParse(plannedEndDate, ukCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate.ToString("dd MMMM yyyy");
        }

        return null;
    }
}