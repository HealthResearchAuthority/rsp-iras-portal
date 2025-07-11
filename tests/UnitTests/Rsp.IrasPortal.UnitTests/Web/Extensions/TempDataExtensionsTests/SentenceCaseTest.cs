using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.UnitTests.Web.Extensions.TempDataExtensionsTests;

public class SentenceCaseExtensionTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("h", "H")]
    [InlineData("H", "H")]
    [InlineData("hello", "Hello")]
    [InlineData("HELLO", "Hello")]
    [InlineData(" hELLO", "Hello")]
    [InlineData("hELLO WORLD", "Hello world")]
    [InlineData("  hELLO WoRLD  ", "Hello world")]
    [InlineData("123abc", "123abc")]
    [InlineData("a", "A")]
    public void ToSentenceCase_ShouldReturnExpectedResult(string input, string expected)
    {
        // Act
        var result = input.ToSentenceCase();

        // Assert
        result.ShouldBe(expected);
    }
}