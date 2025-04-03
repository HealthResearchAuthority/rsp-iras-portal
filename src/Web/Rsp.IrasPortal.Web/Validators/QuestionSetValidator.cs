using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

[ExcludeFromCodeCoverage]
public class QuestionSetValidator : AbstractValidator<QuestionnaireViewModel>
{
    public QuestionSetValidator()
    {
        // Validate all questions in the questionnaire
        RuleForEach(x => x.Questions)
            .SetValidator(new QuestionViewModelDataValidator());
    }
}