using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.ValidationAttributes;

namespace Rsp.IrasPortal.UnitTests.Web.ValidationAttributes;

public class RequiredIfTrueAttributeTests
{
    private class TestModel
    {
        public string? ConditionalProperty { get; set; } = null!;

        [RequiredIfTrue("ConditionalProperty", "Yes")]
        public string? TargetProperty { get; set; }
    }

    [Fact]
    public void Should_FailValidation_When_ConditionalPropertyIsTrue_And_TargetPropertyIsNull()
    {
        // Arrange
        var model = new TestModel { ConditionalProperty = "Yes", TargetProperty = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.TargetProperty) };
        var attribute = new RequiredIfTrueAttribute("ConditionalProperty", "Yes");

        // Act
        var result = attribute.GetValidationResult(model.TargetProperty, context);

        // Assert
        result.ShouldNotBe(ValidationResult.Success);
        result!.ErrorMessage.ShouldBe("The field is required when ConditionalProperty is true.");
    }

    [Fact]
    public void Should_PassValidation_When_ConditionalPropertyIsFalse_IrrespectiveOfTargetProperty()
    {
        // Arrange
        var model = new TestModel { ConditionalProperty = "No", TargetProperty = null }; // TargetProperty is null, but should not be required
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.TargetProperty) };
        var attribute = new RequiredIfTrueAttribute("ConditionalProperty", "Yes");

        // Act
        var result = attribute.GetValidationResult(model.TargetProperty, context);

        // Assert
        result.ShouldBe(ValidationResult.Success);
    }
}