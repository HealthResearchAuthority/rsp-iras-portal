using FluentValidation.TestHelper;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.AuthoriseModificationsOutcomeViewModelValidatorTests;

public class AuthoriseModificationsOutcomeViewModelValidatorTests : TestServiceBase<AuthoriseModificationsOutcomeViewModelValidator>
{
    private readonly AuthoriseModificationsOutcomeViewModelValidator _sut
        = new AuthoriseModificationsOutcomeViewModelValidator();

    private static AuthoriseModificationsOutcomeViewModel Vm(
        string outcome,
        string revisionDesc = null,
        string reason = null)
        => new()
        {
            Outcome = outcome,
            RevisionDescription = revisionDesc,
            ReasonNotApproved = reason
        };

    // -------------------------------------------------------------------------
    // REQUEST REVISIONS TESTS (1000 max, required)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RevisionDescription_RequestRevisions_Required_Error(string value)
    {
        var model = Vm("RequestRevisions", value);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
            .WithErrorMessage("Enter a description of revisions you want to request");
    }

    [Fact]
    public void RevisionDescription_RequestRevisions_Exactly1000_Valid()
    {
        var model = Vm("RequestRevisions", new string('a', 1000));

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.RevisionDescription);
    }

    [Fact]
    public void RevisionDescription_RequestRevisions_Over1000_Error()
    {
        var model = Vm("RequestRevisions", new string('a', 1001));

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
            .WithErrorMessage("The description must be between 1 and 1000 characters");
    }

    // -------------------------------------------------------------------------
    // REVISE AND AUTHORISE TESTS (500 max, required)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RevisionDescription_ReviseAndAuthorise_Required_Error(string value)
    {
        var model = Vm("ReviseAndAuthorise", value);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
            .WithErrorMessage("Enter a description of revisions you want to request");
    }

    [Fact]
    public void RevisionDescription_ReviseAndAuthorise_Exactly500_Valid()
    {
        var model = Vm("ReviseAndAuthorise", new string('a', 500));

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.RevisionDescription);
    }

    [Fact]
    public void RevisionDescription_ReviseAndAuthorise_Over500_Error()
    {
        var model = Vm("ReviseAndAuthorise", new string('a', 501));

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
            .WithErrorMessage("The description must be between 1 and 500 characters");
    }

    // -------------------------------------------------------------------------
    // OTHER OUTCOMES (no rules applied)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("something")]
    public void RevisionDescription_OtherOutcome_NoValidation(string value)
    {
        var model = Vm("Approved", value);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.RevisionDescription);
    }

    // -------------------------------------------------------------------------
    // NOT AUTHORISED TESTS (500 max, required)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReasonNotApproved_NotAuthorised_Required_Error(string value)
    {
        var model = Vm("NotAuthorised", reason: value);

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ReasonNotApproved)
            .WithErrorMessage("Enter a reason for not authorising the modification");
    }

    [Fact]
    public void ReasonNotApproved_NotAuthorised_Exactly500_Valid()
    {
        var model = Vm("NotAuthorised", reason: new string('a', 500));

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNotApproved);
    }

    [Fact]
    public void ReasonNotApproved_NotAuthorised_Over500_Error()
    {
        var model = Vm("NotAuthorised", reason: new string('a', 501));

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ReasonNotApproved)
            .WithErrorMessage("The reason must be between 1 and 500 characters");
    }

    // -------------------------------------------------------------------------
    // OTHER OUTCOMES — reasonNotApproved ignored
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("some text")]
    public void ReasonNotApproved_OtherOutcome_NoValidation(string value)
    {
        var model = Vm("Approved", reason: value);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNotApproved);
    }
}