using FluentValidation;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SponsorOrganisationUserModelValidator : AbstractValidator<SponsorOrganisationUserModel>
{
    private const string AdministratorNotAuthoriserErrorMessage = "Select 'Yes' for the Authoriser if the user has the Organisation Administrator role.";

    public SponsorOrganisationUserModelValidator()
    {
        // Validate Role is Organisation Admin but not Authorizer
        RuleFor(x => x.SponsorOrganisationUser.IsAuthoriser)
            .Equal(true)
            .When(x => x.SponsorOrganisationUser.SponsorRole == Roles.OrganisationAdministrator)
            .WithMessage(AdministratorNotAuthoriserErrorMessage);
    }
}