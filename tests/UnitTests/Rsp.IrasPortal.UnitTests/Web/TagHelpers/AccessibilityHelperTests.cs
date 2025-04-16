using Rsp.IrasPortal.Web.TagHelpers;

namespace Rsp.IrasPortal.UnitTests.Web.TagHelpers;

public class AccessibilityHelperTests
{
    [Theory]
    [InlineData("Planned end date", "planned-end-date-hint")]
    [InlineData("Chief Investigator!", "chief-investigator-hint")]
    [InlineData("Short project title*", "short-project-title-hint")]
    [InlineData("Special_Chars_&_More", "special-chars---more-hint")]
    [InlineData("Spaces   and tabs", "spaces---and-tabs-hint")]
    [InlineData("MixedCASE123", "mixedcase123-hint")]
    [InlineData("Title (UK)", "title--uk-hint")]
    [InlineData("---Already-Hyphenated---", "already-hyphenated-hint")]
    [InlineData("   ", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void BuildAriaDescribedBy_ReturnsExpectedValue(string input, string expected)
    {
        // Act
        var result = AccessibilityHelper.BuildAriaDescribedBy(input);

        // Assert
        Assert.Equal(expected, result);
    }
}