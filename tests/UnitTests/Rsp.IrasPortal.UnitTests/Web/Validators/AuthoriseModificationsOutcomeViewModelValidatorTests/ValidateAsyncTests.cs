using FluentValidation.TestHelper;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.AuthoriseModificationsOutcomeViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AuthoriseModificationsOutcomeViewModelValidator>
{
    private static AuthoriseModificationsOutcomeViewModel Vm(string? desc) =>
        new AuthoriseModificationsOutcomeViewModel { RevisionDescription = desc };

    private readonly AuthoriseModificationsOutcomeViewModelValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WhenRevisionDescription_IsExactly1000Chars_ShouldBe_Valid()
    {
        // Arrange
        var exactly1000 = new string('x', 1000);
        var model = Vm(exactly1000);

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ValidateAsync_WhenRevisionDescription_IsValidText_ShouldBe_Valid()
    {
        // Arrange
        var model = Vm("Please outline the revisions required in section 2 and 3.");

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void RevisionDescription_WhenOutcomeIsRequestRevisions_AndNull_ShouldHaveRequiredError()
    {
        var model = Model(outcome: "RequestRevisions", revisionDescription: null);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
              .WithErrorMessage("Enter a description of revisions you want to request");
    }

    [Theory]
    [InlineData("")]                 // empty
    [InlineData("   ")]              // whitespace
    public void RevisionDescription_WhenOutcomeIsRequestRevisions_AndBlank_ShouldHaveRequiredError(string value)
    {
        var model = Model(outcome: "RequestRevisions", revisionDescription: value);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
              .WithErrorMessage("Enter a description of revisions you want to request");
    }

    [Fact]
    public void RevisionDescription_WhenOutcomeIsRequestRevisions_AndExactly1000_IsValid()
    {
        var text = new string('a', 1000);
        var model = Model(outcome: "RequestRevisions", revisionDescription: text);

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.RevisionDescription);
        // Also ensure no custom excess-char failure added
        result.ShouldNotHaveValidationErrorFor("_DescriptionExcessCharacterCount");
    }

    [Fact]
    public void RevisionDescription_WhenOutcomeIsRequestRevisions_AndOver1000_HasMaxLengthMessage_AndCustomExcessFailure()
    {
        var text = new string('a', 1001);
        var model = Model(outcome: "RequestRevisions", revisionDescription: text);

        var result = _validator.TestValidate(model);

        // Standard max-length message
        result.ShouldHaveValidationErrorFor(x => x.RevisionDescription)
              .WithErrorMessage("The description must be 1000 characters or less");

        // Custom failure key/value
        var excess = 1001 - 1000;
        result.ShouldHaveValidationErrorFor("_DescriptionExcessCharacterCount")
              .WithErrorMessage($"You have exceeded the characters limits by {excess}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("something")]
    public void RevisionDescription_WhenOutcomeIsNotRequestRevisions_NoRuleTriggered(string value)
    {
        var model = Model(outcome: "SomeOtherOutcome", revisionDescription: value);

        var result = _validator.TestValidate(model);

        // The conditional rule `.When(x => x.Outcome == "RequestRevisions")` should not apply:
        result.ShouldNotHaveValidationErrorFor(x => x.RevisionDescription);
        result.ShouldNotHaveValidationErrorFor("_DescriptionExcessCharacterCount");
    }

    [Fact]
    public void RevisionDescription_CustomRule_StillAddsExcessFailureWhenOutcomeNotRequestRevisions()
    {
        // IMPORTANT: In your current validator, the second Custom rule for RevisionDescription
        // does NOT have a .When(...) condition and will always run.
        // If that’s intentional, this test shows it adds excess failure regardless of outcome.
        // If you intended it to be conditional, add `.When(x => x.Outcome == "RequestRevisions")`
        // to the Custom rule in your validator and adjust this test accordingly.

        var text = new string('a', 1001);
        var model = Model(outcome: "DifferentOutcome", revisionDescription: text);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("_DescriptionExcessCharacterCount")
              .WithErrorMessage("You have exceeded the characters limits by 1");
    }

    // ---------------------------
    // ReasonNotApproved rules
    // ---------------------------

    [Fact]
    public void ReasonNotApproved_WhenOutcomeIsNotAuthorised_AndNull_ShouldHaveRequiredError()
    {
        var model = Model(outcome: "NotAuthorised", reasonNotApproved: null);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ReasonNotApproved)
              .WithErrorMessage("Enter a reason for not authorising the modification");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReasonNotApproved_WhenOutcomeIsNotAuthorised_AndBlank_ShouldHaveRequiredError(string value)
    {
        var model = Model(outcome: "NotAuthorised", reasonNotApproved: value);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ReasonNotApproved)
              .WithErrorMessage("Enter a reason for not authorising the modification");
    }

    [Fact]
    public void ReasonNotApproved_WhenOutcomeIsNotAuthorised_AndExactly500_IsValid()
    {
        var text = new string('x', 500);
        var model = Model(outcome: "NotAuthorised", reasonNotApproved: text);

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNotApproved);
        result.ShouldNotHaveValidationErrorFor("_DescriptionExcessCharacterCount");
    }

    [Fact]
    public void ReasonNotApproved_WhenOutcomeIsNotAuthorised_AndOver500_HasMaxLengthMessage_AndCustomExcessFailure()
    {
        var text = new string('x', 501);
        var model = Model(outcome: "NotAuthorised", reasonNotApproved: text);

        var result = _validator.TestValidate(model);

        // Standard max-length message
        result.ShouldHaveValidationErrorFor(x => x.ReasonNotApproved)
              .WithErrorMessage("The reason must be 500 characters or less");

        // Custom failure
        var excess = 501 - 500;
        result.ShouldHaveValidationErrorFor("_DescriptionExcessCharacterCount")
              .WithErrorMessage($"You have exceeded the characters limits by {excess}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("something")]
    public void ReasonNotApproved_WhenOutcomeIsNotNotAuthorised_NoRuleTriggered(string value)
    {
        var model = Model(outcome: "SomeOtherOutcome", reasonNotApproved: value);

        var result = _validator.TestValidate(model);

        // Conditional rule `.When(x => x.Outcome == "NotAuthorised")` should not apply:
        result.ShouldNotHaveValidationErrorFor(x => x.ReasonNotApproved);
        result.ShouldNotHaveValidationErrorFor("_DescriptionExcessCharacterCount");
    }

    [Fact]
    public void ReasonNotApproved_CustomRule_StillAddsExcessFailureWhenOutcomeNotNotAuthorised()
    {
        // As with RevisionDescription, the Custom rule for ReasonNotApproved
        // currently runs unconditionally. This test reflects that behavior.
        // If that’s NOT desired, add `.When(x => x.Outcome == "NotAuthorised")`
        // to the Custom rule in your validator and update this test.

        var text = new string('x', 501);
        var model = Model(outcome: "DifferentOutcome", reasonNotApproved: text);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("_DescriptionExcessCharacterCount")
              .WithErrorMessage("You have exceeded the characters limits by 1");
    }

    private static AuthoriseModificationsOutcomeViewModel Model(
                string outcome = null,
                string revisionDescription = null,
                string reasonNotApproved = null)
                => new AuthoriseModificationsOutcomeViewModel
                {
                    Outcome = outcome,
                    RevisionDescription = revisionDescription,
                    ReasonNotApproved = reasonNotApproved
                };
}