using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Features.Approvals.RecordSearch.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.SearchRecordNavigationValidatorTests;

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