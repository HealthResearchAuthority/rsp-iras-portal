using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.UnitTests.Web.Extensions.TempDataExtensionsTests;

public class GetLabelTextTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("nhs", "NHS")]
    [InlineData("hsc", "HSC")]
    [InlineData("nct", "NCT")]
    [InlineData("isrctn", "ISRCTN")]
    [InlineData("NHS and hsc", "NHS and HSC")]
    [InlineData("chief investigator", "Chief Investigator")]
    [InlineData("principal investigator", "Principal Investigator")]
    [InlineData("nhs chief investigator", "NHS Chief Investigator")]
    [InlineData("HSC principal investigator", "HSC Principal Investigator")]
    [InlineData("random text", "random text")]
    [InlineData("nhs and principal investigator", "NHS and Principal Investigator")]
    public void GetLabelText_ShouldReturnExpectedResult(string input, string expected)
    {
        // Act
        var result = input.GetLabelText();

        // Assert
        result.ShouldBe(expected);
    }
}