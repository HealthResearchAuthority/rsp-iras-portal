using Rsp.Portal.Web.Helpers;

namespace Rsp.IrasPortal.UnitTests.Web.Helpers;

public class DateHelperTests
{
    [Fact]
    public void ConvertDateToString_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = DateHelper.ConvertDateToString((DateTime?)null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ConvertDateToString_ValidDate_ReturnsFormattedDate()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15);

        // Act
        var result = DateHelper.ConvertDateToString(date);

        // Assert
        result.ShouldBe("15 March 2024");
    }

    [Fact]
    public void ConvertDateToString_ValidDate_UsesEnGbFormatting()
    {
        // Arrange
        var date = new DateTime(2025, 12, 2);

        // Act
        var result = DateHelper.ConvertDateToString(date);

        // Assert
        result.ShouldBe("02 December 2025");
    }

    [Theory]
    [InlineData(2023, 1, 1, "01 January 2023")]
    [InlineData(2023, 7, 9, "09 July 2023")]
    [InlineData(2023, 10, 31, "31 October 2023")]
    public void ConvertDateToString_FormatsVariousDatesCorrectly(int year, int month, int day, string expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var result = DateHelper.ConvertDateToString(date);

        // Assert
        result.ShouldBe(expected);
    }
}