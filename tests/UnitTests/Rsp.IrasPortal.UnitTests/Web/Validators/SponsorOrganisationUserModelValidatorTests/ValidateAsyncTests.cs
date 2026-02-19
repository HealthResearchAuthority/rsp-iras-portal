using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Validators;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.SponsorOrganisationUserModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<SponsorOrganisationUserModelValidator>
{
    private const string ErrorMessage =
        "Select 'Yes' for the Authoriser if the user has the Organisation Administrator role.";

    [Fact]
    public async Task ShouldReturnError_WhenOrgAdmin_AndIsAuthoriserFalse()
    {
        // Arrange
        var model = new SponsorOrganisationUserModel
        {
            SponsorOrganisationUser = new SponsorOrganisationUserDto
            {
                SponsorRole = Roles.OrganisationAdministrator,
                IsAuthoriser = false
            }
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SponsorOrganisationUser.IsAuthoriser)
              .WithErrorMessage(ErrorMessage);
    }

    [Fact]
    public async Task ShouldPass_WhenOrgAdmin_AndIsAuthoriserTrue()
    {
        // Arrange
        var model = new SponsorOrganisationUserModel
        {
            SponsorOrganisationUser = new SponsorOrganisationUserDto
            {
                SponsorRole = Roles.OrganisationAdministrator,
                IsAuthoriser = true
            }
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SponsorOrganisationUser.IsAuthoriser);
    }

    [Fact]
    public async Task ShouldPass_WhenNotOrgAdmin_AndIsAuthoriserFalse()
    {
        // Arrange
        var model = new SponsorOrganisationUserModel
        {
            SponsorOrganisationUser = new SponsorOrganisationUserDto
            {
                SponsorRole = Roles.Sponsor, // dowolna inna rola niż OrganisationAdministrator
                IsAuthoriser = false
            }
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SponsorOrganisationUser.IsAuthoriser);
    }

    [Fact]
    public async Task ShouldPass_WhenNotOrgAdmin_AndIsAuthoriserTrue()
    {
        // Arrange
        var model = new SponsorOrganisationUserModel
        {
            SponsorOrganisationUser = new SponsorOrganisationUserDto
            {
                SponsorRole = Roles.Sponsor,
                IsAuthoriser = true
            }
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SponsorOrganisationUser.IsAuthoriser);
    }
}