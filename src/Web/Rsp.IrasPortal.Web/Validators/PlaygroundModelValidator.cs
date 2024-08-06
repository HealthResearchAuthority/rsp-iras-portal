using FluentValidation;
using Rsp.IrasPortal.Domain.Entities;

namespace Rsp.IrasPortal.Domain.Validators;

public class PlaygroundModelValidator : AbstractValidator<PlaygroundModel>
{
    public PlaygroundModelValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
    }
}