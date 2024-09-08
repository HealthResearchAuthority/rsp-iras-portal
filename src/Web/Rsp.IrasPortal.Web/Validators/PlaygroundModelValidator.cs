using FluentValidation;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Domain.Validators;

public class PlaygroundModelValidator : AbstractValidator<PlaygroundModel>
{
    public PlaygroundModelValidator()
    {
        RuleFor(x => x.ShortProjectTitle).NotEmpty().Length(3, 20);
        RuleFor(x => x.IrasProjectId).NotEmpty().Matches(@"^[0-9]{6}$");
        RuleFor(x => x.ChiefInvestigator).NotEmpty();
        RuleFor(x => x.ReviewedByRec).NotEmpty();
    }
}