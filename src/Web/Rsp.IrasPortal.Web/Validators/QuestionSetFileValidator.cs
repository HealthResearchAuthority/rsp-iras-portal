using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionSetFileValidator : AbstractValidator<QuestionSetFileModel>
{
    public QuestionSetFileValidator()
    {
        RuleForEach(x => x.QuestionDtos)
            .SetValidator(new QuestionDtoValidator());
    }
}