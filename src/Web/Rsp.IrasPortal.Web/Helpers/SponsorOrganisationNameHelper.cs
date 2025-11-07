using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Helpers;

public static class SponsorOrganisationNameHelper
{
    /// <summary>
    /// Gets the name of sponsor organisation for the project from RTS service, if answer for sponsor organisation question is provided
    /// </summary>
    /// <returns>
    /// The name of sponsor organisation of the project, or null if the question/answer is not found.
    /// </returns>
    public static async Task<string?> GetSponsorOrganisationNameFromQuestions(IRtsService rtsService, IEnumerable<QuestionViewModel> questions, bool getIdFromQuestionsFirstAnswer = false)
    {
        var sponsorOrgInput = questions.FirstOrDefault(q => string.Equals(q.QuestionType, "rts:org_lookup", StringComparison.OrdinalIgnoreCase));
        var organisationId = getIdFromQuestionsFirstAnswer ? sponsorOrgInput?.Answers?.FirstOrDefault()?.AnswerText : sponsorOrgInput?.AnswerText;
        return await GetSponsorOrganisationNameFromOrganisationId(rtsService, organisationId);
    }

    /// <summary>
    /// Gets the name of sponsor organisation for the project from RTS service, if organisation Id is provided
    /// </summary>
    /// <returns>
    /// The name of sponsor organisation of the project, or null if the question/answer is not found.
    /// </returns>
    public static async Task<string?> GetSponsorOrganisationNameFromOrganisationId(IRtsService rtsService, string? organisationId)
    {
        string? sponsorOrganisationName = null;
        if (organisationId is not null)
        {
            var orgResponse = await rtsService.GetOrganisation(organisationId);
            if (orgResponse is not null && orgResponse.IsSuccessStatusCode)
            {
                sponsorOrganisationName = orgResponse.Content?.Name;
            }
        }
        return sponsorOrganisationName;
    }
}