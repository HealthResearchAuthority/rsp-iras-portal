using System.Text.RegularExpressions;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Helpers;

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

    public static async Task GetSponsorOrganisationsNameForAuditRecords(
        IRtsService rtsService,
        IEnumerable<object> auditRecords)
    {
        // Regex:
        // Primary sponsor organisation changed from {int} to {int}
        var regex = new Regex(
            @"^Primary sponsor organisation changed from '(\d+)' to '(\d+)'",
            RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

        foreach (var record in auditRecords)
        {
            var descProp = record.GetType().GetProperty("Description");
            if (descProp == null)
            {
                continue;
            }

            var desc = descProp.GetValue(record) as string;
            if (string.IsNullOrWhiteSpace(desc))
            {
                continue;
            }

            var match = regex.Match(desc);
            if (!match.Success)
            {
                continue;
            }

            string firstIdString = match.Groups[1].Value;
            string secondIdString = match.Groups[2].Value;

            var firstOrgName = await GetSponsorOrganisationNameFromOrganisationId(rtsService, firstIdString);
            var secondOrgName = await GetSponsorOrganisationNameFromOrganisationId(rtsService, secondIdString);

            var newDesc = desc;
            if (firstOrgName != null)
            {
                newDesc = newDesc.Replace(firstIdString, firstOrgName);
            }

            if (secondOrgName != null)
            {
                newDesc = newDesc.Replace(secondIdString, secondOrgName);
            }

            descProp.SetValue(record, newDesc);
        }
    }
}