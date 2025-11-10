using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Helpers;

public static class ModificationHelpers
{
    /// <summary>
    /// Finds and processes a surfacing question in the provided list of questions.
    /// If a surfacing question is found and its <c>ShowAnswerOn</c> property contains the specified <paramref name="actionName"/>,
    /// the question is removed from the list and its display text is assigned to <paramref name="modificationChange"/>.
    /// </summary>
    /// <param name="questions">The list of <see cref="QuestionViewModel"/> to search for a surfacing question.</param>
    /// <param name="modificationChange">The <see cref="ModificationChangeModel"/> to update with the surfacing question's answer text.</param>
    /// <param name="actionName">The action name to check against the surfacing question's <c>ShowAnswerOn</c> property.</param>
    public static void ShowSurfacingQuestion(List<QuestionViewModel> questions, ModificationChangeModel modificationChange, string actionName)
    {
        // get the question to check if the answer to the question should be shown
        var surfacingQuestion = questions.Find(q => !string.IsNullOrWhiteSpace(q.ShowAnswerOn));
        var showSurfacingQuestion = surfacingQuestion?.ShowAnswerOn.Contains(actionName, StringComparison.OrdinalIgnoreCase) == true;

        // remove the question from the list if we are showing the surfacing question
        if (showSurfacingQuestion && surfacingQuestion != null)
        {
            questions.RemoveAll(q => q.QuestionId == surfacingQuestion!.QuestionId);
            // If the surfacing question should be shown for ModificationDetails, capture its display text

            modificationChange.SpecificChangeAnswer = surfacingQuestion.GetDisplayText(false);
        }
    }

    public static RankingOfChangeRequest GetRankingOfChangeRequest
    (
        string specificAreaOfChangeId,
        bool applicability,
        IEnumerable<QuestionViewModel> questions,
        string nhsOrHscOrganisations
    )
    {
        QuestionViewModel? nhsInvolvmentQuestion = null;

        if (nhsOrHscOrganisations == "Yes")
        {
            // When NHS or HSC organisations are involved, explicitly mark as involved
            nhsInvolvmentQuestion = new QuestionViewModel();
        }
        else
        {
            // Otherwise, check if the question indicates NHS involvement
            nhsInvolvmentQuestion = questions
                .SingleOrDefault(q =>
                    q.NhsInvolvment != null &&
                    q.Answers?.Any(a => a.IsSelected && a.AnswerText == q.NhsInvolvment) == true);
        }

        // Non-NHS involvement question (safe lookup)
        var nonNhsInvolvmentQuestion = questions
            .SingleOrDefault(q =>
                q.NonNhsInvolvment != null &&
                q.Answers?.Any(a => a.IsSelected && a.AnswerText == q.NonNhsInvolvment) == true);

        // Organisation and resource questions
        var orgsAffectedQuestion = questions.SingleOrDefault(q => q.AffectedOrganisations);
        var additionaResourcesQuestion = questions.SingleOrDefault(q => q.RequireAdditionalResources);

        // Default flags
        var isNhsInvolved = nhsInvolvmentQuestion is not null;
        var nhsOrganisationsAffected = string.Empty;

        if (nhsOrHscOrganisations == "Yes")
        {
            // Force both flags true if NHS involvement explicitly confirmed
            isNhsInvolved = true;
            nhsOrganisationsAffected = "All";
        }
        else
        {
            // Otherwise, infer from the question if possible
            nhsOrganisationsAffected = orgsAffectedQuestion?
                .Answers?
                .FirstOrDefault(a => a.AnswerId == orgsAffectedQuestion?.SelectedOption)?
                .AnswerText;
        }

        return new RankingOfChangeRequest
        {
            SpecificAreaOfChangeId = specificAreaOfChangeId,
            Applicability = applicability ? "Yes" : "No",
            IsNHSInvolved = isNhsInvolved,
            IsNonNHSInvolved = nonNhsInvolvmentQuestion is not null,
            NhsOrganisationsAffected = nhsOrganisationsAffected,
            NhsResourceImplicaitons =
                string.Equals(
                    additionaResourcesQuestion?
                        .Answers?
                        .FirstOrDefault(a => a.AnswerId == additionaResourcesQuestion?.SelectedOption)?
                        .AnswerText,
                    "Yes",
                    StringComparison.OrdinalIgnoreCase)
        };
    }
}