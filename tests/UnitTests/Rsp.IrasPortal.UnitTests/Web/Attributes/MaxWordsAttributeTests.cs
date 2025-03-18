using System.ComponentModel.DataAnnotations;
using Rsp.IrasPortal.Web.Attributes;

public class MaxWordsAttributeTests
{
    [Theory]
    [InlineData("This is a test", 4, true)] // Exactly at the limit
    [InlineData("One two three four five", 4, false)] // Exceeds limit
    [InlineData("Word", 1, true)] // Single word within limit
    [InlineData("Word1 Word2", 2, true)] // Exactly at the limit
    [InlineData("Word1 Word2 Word3", 2, false)] // Exceeds limit
    [InlineData("   Leading and trailing spaces   ", 4, true)] // Trims and counts correctly
    [InlineData("", 2, true)] // Empty string should be valid
    [InlineData(null, 3, true)] // Null should be valid
    [InlineData("    ", 3, true)] // Whitespace only should be valid
    public void IsValid_ShouldReturnExpectedResult(string input, int maxWords, bool expectedIsValid)
    {
        // Arrange
        var attribute = new MaxWordsAttribute(maxWords);
        var validationContext = new ValidationContext(new { });

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
            result.ErrorMessage.ShouldBe($"The field can have a maximum of {maxWords} words.");
        }
    }
}