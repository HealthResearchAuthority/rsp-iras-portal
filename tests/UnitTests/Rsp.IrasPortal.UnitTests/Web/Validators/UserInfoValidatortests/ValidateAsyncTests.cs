﻿using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.UserInfoValidatortests;

public class ValidateAsyncTests : TestServiceBase<UserInfoValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyName()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = null!,
            FamilyName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage("Enter a first name");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTelephoneNoDigit()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "Hello",
            FamilyName = "Ham",
            Telephone = "qwertyuiopa"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must only contain numbers");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTelephone11DigitOrMore()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "Hello",
            FamilyName = "Ham",
            Telephone = "078987654323"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must be 11 digits or less");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            UserRoles = [ new()
            {
                 Name = "operations",
                 IsSelected = true
            }],
            Country = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("You must provide a country");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsNotSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            UserRoles =
            [ new()
                {
                    Name = "admin"
                }
            ],
            Country = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public async Task ShouldPassValidationWhenBothNameAndDescriptionAreProvided()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "John",
            FamilyName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GivenName);
        result.ShouldNotHaveValidationErrorFor(x => x.FamilyName);
    }

    [Theory]
    [InlineData("john.doe@example.com")]
    [InlineData("john_doe@example.com")]
    [InlineData("john-doe@example.com")]
    [InlineData("john+newsletter@example.com")]
    [InlineData("john!doe@example.com")]
    [InlineData("john#profile@example.com")]
    [InlineData("john$invoice@example.com")]
    [InlineData("john%discount@example.com")]
    [InlineData("john&team@example.com")]
    [InlineData("john's@example.com")]
    [InlineData("john*star@example.com")]
    [InlineData("john/everyone@example.com")]
    [InlineData("john=equals@example.com")]
    [InlineData("john?query@example.com")]
    [InlineData("john^caret@example.com")]
    [InlineData("john`code@example.com")]
    [InlineData("john{editor}@example.com")]
    [InlineData("john|vertical@example.com")]
    [InlineData("john~tilde@example.com")]
    [InlineData("johñ.unicode@example.com")]
    [InlineData("john!#$%&'*+-/=?^_`{|}~doe@example.com")] // (uses special characters allowed in the local part)
    [InlineData("john-_doe@example.com")] // (combination of valid special characters)
    [InlineData("mail.sub.subsub.sub@example.com")] // (nested subdomains within valid limits)
    [InlineData("john.doe@example.photography")]
    [InlineData("Johndoe2@example.com")]
    [InlineData("MaxEmailLength64LocalAddressMaxEmailLength64LocalAddressMaxEmail@MaxEmailLength63SecondDomainAddressMaxEmailLength63SecondDomain.MaxEmailLength63SecondDomainAddressMaxEmailLength63SecondDomain.MaxEmailLengthXXSecondDomainAddressMaxEmailLengthXXSecondDoma")]
    public async Task ShouldNotHaveValidationErrorForValidEmail(string email)
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "John",
            FamilyName = "Doe",
            Email = email
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(".john@example.com")] // (leading dot)
    [InlineData("john.@example.com")] // (trailing dot)
    [InlineData("john..doe@example.com")] // (double dots)
    [InlineData("john doe@example.com")] // (contains a space)
    [InlineData("john[at]example.com")] // (contains brackets)
    [InlineData("john<doe>@example.com")] // (contains angle brackets)
    [InlineData("john:doe@example.com")] // (contains a colon)
    [InlineData("john;doe@example.com")] // (contains a semicolon)
    [InlineData("john,@example.com")] // (contains a comma)
    [InlineData("john@-example.com")] // (leading hyphen in domain part)
    [InlineData("john@example-.com")] // (trailing hyphen in domain part)
    [InlineData("john.@example..com")] // (double dots in domain part)
    [InlineData("john!doe@exa!mple.com")] // (invalid special character in domain part)
    [InlineData("\"john.doe\"@example.com")] // (quoted string unsupported by most providers)
    [InlineData(" john.doe@example.com")] // (space before the local part)
    [InlineData("averyveryverylongdomainnamewithmanysubdomainsandmorecharacters.com")] // (domain exceeds 253-character limit)
    [InlineData("john@example..com")] // (consecutive dots in the domain)
    [InlineData("mail..sub.example.com")] // (contains consecutive dots in the subdomain)
    [InlineData("john..doe@example..com")] // (consecutive dots in both local and domain parts)
    [InlineData("john🙂@example.com")] // (emoji not allowed in the local part)
    [InlineData("john.doe@example.1234")] // invalid TLD - Incorrectly accepted by system (as valid)
    [InlineData("john.doeexample.com")] // (missing @ symbol)
    [InlineData("john.doe@localhost")] // (reserved domain)
    [InlineData("MoreThan64CharactersForTheLocalAddressBeforeTheAtSymbolInAnEmailA@example.com")] // (more than 64 characters for the local address before the @ symbol)
    [InlineData("MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfive@MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfiveMoreThan320CharactersForTheWholeEmailAddressonetwothreefourfiveMoreThan320CharactersForTheWholeEmailAddressonetwothreefourfivesixs.MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfive")]
    public async Task ShouldHaveValidationErrorForInvalidEmail(string email)
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "John",
            FamilyName = "Doe",
            Email = email
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}