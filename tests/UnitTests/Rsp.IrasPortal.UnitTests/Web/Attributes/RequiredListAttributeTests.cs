using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.Attributes;

namespace Rsp.IrasPortal.UnitTests.Web.Attributes;

public class RequiredListAttributeTests
{
    [Theory]
    [InlineData(new[] { "England" }, true)] // Valid list with one item
    [InlineData(new[] { "England", "Scotland" }, true)] // Valid list with multiple items
    [InlineData(new string[] { }, false)] // Empty list
    public void IsValid_ShouldReturnExpectedResult(string[] inputArray, bool expectedIsValid)
    {
        // Arrange
        var attribute = new RequiredListAttribute();
        var validationContext = new ValidationContext(new { });
        var input = inputArray.Length > 0 ? new List<string>(inputArray) : new List<string>();

        // Act
        var result = attribute.GetValidationResult(input, validationContext);

        // Assert
        if (expectedIsValid)
        {
            result.ShouldBe(ValidationResult.Success);
        }
        else
        {
            result.ShouldNotBe(ValidationResult.Success);
            result.ErrorMessage.ShouldBe("Select at least one country.");
        }
    }

    [Fact]
    public void IsValid_ShouldReturnError_ForNullList()
    {
        // Arrange
        var attribute = new RequiredListAttribute();
        var validationContext = new ValidationContext(new { });

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result.ErrorMessage.ShouldBe("Select at least one country.");
    }
}