using System.Reflection;
using Rsp.IrasPortal.Web.Helpers;

namespace Rsp.IrasPortal.UnitTests.Web.Helpers;

public class DateHelperTests
{
    [Fact]
    public void GetFormattedDateWithOrdinal_ShouldReturnEmptyString_WhenDateIsNull()
    {
        // Act
        var result = DateHelper.GetFormattedDateWithOrdinal(null);

        // Assert
        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, "1st March 2025 13:50")]
    [InlineData(2, "2nd March 2025 13:50")]
    [InlineData(3, "3rd March 2025 13:50")]
    [InlineData(4, "4th March 2025 13:50")]
    [InlineData(11, "11th March 2025 13:50")]
    [InlineData(12, "12th March 2025 13:50")]
    [InlineData(13, "13th March 2025 13:50")]
    [InlineData(21, "21st March 2025 13:50")]
    [InlineData(22, "22nd March 2025 13:50")]
    [InlineData(23, "23rd March 2025 13:50")]
    [InlineData(24, "24th March 2025 13:50")]
    public void GetFormattedDateWithOrdinal_ShouldReturnCorrectlyFormattedDate(int day, string expected)
    {
        // Arrange
        var testDate = new DateTime(2025, 3, day, 13, 50, 0);

        // Act
        var result = DateHelper.GetFormattedDateWithOrdinal(testDate);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "st")]
    [InlineData(2, "nd")]
    [InlineData(3, "rd")]
    [InlineData(4, "th")]
    [InlineData(11, "th")]
    [InlineData(12, "th")]
    [InlineData(13, "th")]
    [InlineData(21, "st")]
    [InlineData(22, "nd")]
    [InlineData(23, "rd")]
    [InlineData(24, "th")]
    [InlineData(31, "st")]
    public void GetDaySuffix_ShouldReturnCorrectSuffix(int day, string expectedSuffix)
    {
        // Act
        var result = typeof(DateHelper)
            .GetMethod("GetDaySuffix", BindingFlags.NonPublic | BindingFlags.Static)
            ?.Invoke(null, [day]) as string;

        // Assert
        result.ShouldBe(expectedSuffix);
    }
}