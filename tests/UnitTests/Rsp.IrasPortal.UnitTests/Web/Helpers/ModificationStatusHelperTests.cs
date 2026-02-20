using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Helpers;

namespace Rsp.Portal.UnitTests.Web.Helpers;

public class ModificationStatusHelperTests : TestServiceBase
{
    [Theory]
    [InlineData(ModificationStatus.RequestRevisions, ModificationStatus.InDraft)]
    [InlineData(ModificationStatus.ReviseAndAuthorise, ModificationStatus.WithSponsor)]
    [InlineData("SomeOtherStatus", "SomeOtherStatus")]
    [InlineData(null, null)]
    public void ToUiStatus_ReturnsExpectedValue(string? input, string? expected)
    {
        // Act
        var result = ModificationStatusHelper.ToUiStatus(input);

        // Assert
        result.ShouldBe(expected);
    }
}