using FluentValidation;
using Rsp.Portal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class ModificationDetailsViewModelValidator : AbstractValidator<ModificationDetailsViewModel>
{
    private const int MaxCharactersCount = 500;
    private readonly string ApplicantRevisionResponseErrorMessage = "Enter description of revisions made";
    private readonly string ApplicantRevisionResponseCharactersErrorMessage = $"The description of revisions must be between 1 and {MaxCharactersCount} characters";

    public ModificationDetailsViewModelValidator()
    {
        RuleFor(x => x.ApplicantRevisionResponse)
                   .Cascade(CascadeMode.Stop)
                   .Must(s => !string.IsNullOrWhiteSpace(s))
                       .WithMessage(ApplicantRevisionResponseErrorMessage);

        RuleFor(x => x.ApplicantRevisionResponse)
                  .MaximumLength(MaxCharactersCount)
                      .WithMessage(ApplicantRevisionResponseCharactersErrorMessage);
    }
}