using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class ModificationAddDocumentDetailsViewModelValidator : AbstractValidator<ModificationAddDocumentDetailsViewModel>
{
    public ModificationAddDocumentDetailsViewModelValidator()
    {
        RuleFor(x => x.DocumentTypeId)
            .NotNull()
            .WithMessage("Select a document type");

        RuleFor(x => x.SponsorDocumentVersion)
            .NotEmpty()
            .WithMessage("Enter a sponsor document version");

        RuleFor(x => x.HasPreviousVersionApproved)
            .NotNull()
            .WithMessage("Specify if the document has a previous version");

        RuleFor(x => x)
            .Must(HaveValidSponsorDocumentDate)
            .WithMessage("Enter a valid document date")
            .WithName("SponsorDocumentDate");
    }

    private bool HaveValidSponsorDocumentDate(ModificationAddDocumentDetailsViewModel model)
    {
        if (model.SponsorDocumentDateDay is null ||
            model.SponsorDocumentDateMonth is null ||
            model.SponsorDocumentDateYear is null)
        {
            return false;
        }

        return DateTime.TryParse($"{model.SponsorDocumentDateYear}-{model.SponsorDocumentDateMonth}-{model.SponsorDocumentDateDay}", out _);
    }
}