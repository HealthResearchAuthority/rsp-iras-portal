using FluentValidation;
using Rsp.Portal.Web.Features.Approvals.RecordSearch.Models;

namespace Rsp.Portal.Web.Validators;

public class RecordSearchNavigationModelValidator : AbstractValidator<RecordSearchNavigationModel>
{
    private const string RecordTypeMandatoryErrorMessage = "Select a record type";

    public RecordSearchNavigationModelValidator()
    {
        RuleFor(x => x.RecordType)
            .NotEmpty()
            .WithMessage(RecordTypeMandatoryErrorMessage);
    }
}