using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Validators.QuestionSet;

[ExcludeFromCodeCoverage]
public class QuestionSetDtoValidator : AbstractValidator<QuestionSetDto>
{
    public QuestionSetDtoValidator()
    {
        RuleForEach(x => x.Questions)
            .SetValidator(new QuestionDtoValidator());
    }
}