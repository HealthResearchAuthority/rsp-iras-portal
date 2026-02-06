using FluentValidation.TestHelper;
using Rsp.Portal.Web.Features.Approvals.RecordSearch.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.SearchRecordNavigationValidatorTests;

public class ValidateAsyncTests : TestServiceBase<RecordSearchNavigationModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyRecordType()
    {
        // Arrange
        var model = new RecordSearchNavigationModel
        {
            RecordType = null
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RecordType)
            .WithErrorMessage("Select a record type");
    }
}