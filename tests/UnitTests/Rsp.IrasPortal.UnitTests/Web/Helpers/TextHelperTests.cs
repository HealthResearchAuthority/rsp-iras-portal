using Rsp.Portal.Web.Helpers;

namespace Rsp.Portal.UnitTests.Web.Helpers;

public class TextHelperTests : TestServiceBase
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ToSentenceCaseWithAcronyms_ReturnsEmpty_ForNullOrWhitespace(string input, string expected)
    {
        var result = TextHelper.ToSentenceCaseWithAcronyms(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ae", "AE")]
    [InlineData("Ae", "AE")]
    [InlineData("This is an ae file", "this is an AE file")]
    [InlineData("this is a ctimp trial", "this is a CTIMP trial")]
    [InlineData("PIC and pics should match", "PIC and PICs should match")]
    [InlineData("icmje guidelines", "ICMJE guidelines")]
    [InlineData("nres approval required", "NRES approval required")]
    [InlineData("SmPc details here", "SmPC details here")]
    public void ToSentenceCaseWithAcronyms_PreservesAcronyms(string input, string expected)
    {
        var result = TextHelper.ToSentenceCaseWithAcronyms(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSentenceCaseWithAcronyms_LowersNonAcronyms()
    {
        var input = "THIS IS A TEST";
        var result = TextHelper.ToSentenceCaseWithAcronyms(input);
        Assert.Equal("this is a test", result);
    }

    [Fact]
    public void ToSentenceCaseWithAcronyms_MixedContent_HandlesCorrectly()
    {
        var input = "Upload AE and CTIMP documents for this Project";
        var result = TextHelper.ToSentenceCaseWithAcronyms(input);
        Assert.Equal("upload AE and CTIMP documents for this project", result);
    }

    [Theory]
    [InlineData("hs (copi) regs guidance", "HS (COPI) Regs guidance")]
    [InlineData("hs (cpi) regs guidance", "HS (CPI) Regs guidance")]
    public void ToSentenceCaseWithAcronyms_PreservesMultiWordAcronyms(string input, string expected)
    {
        var result = TextHelper.ToSentenceCaseWithAcronyms(input);
        Assert.Equal(expected, result);
    }
}

// Test accessor for acronyms array in TextHelper
internal static class TextHelper_Accessor
{
    public static string[] Acronyms =>
        (string[])typeof(TextHelper)
            .GetField("Acronyms", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .GetValue(null)!;
}