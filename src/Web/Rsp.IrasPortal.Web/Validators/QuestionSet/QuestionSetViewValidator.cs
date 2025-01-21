using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators.QuestionSet;

[ExcludeFromCodeCoverage]
public class QuestionSetViewValidator : AbstractValidator<QuestionSetViewModel>
{
    public QuestionSetViewValidator()
    {
        RuleFor(x => x.QuestionSetDto)
            .SetValidator(new QuestionSetDtoValidator()!);
    }
}