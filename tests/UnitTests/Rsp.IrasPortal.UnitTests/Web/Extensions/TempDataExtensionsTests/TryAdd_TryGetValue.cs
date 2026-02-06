using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Web.Extensions;

namespace Rsp.Portal.UnitTests.Web.Extensions.TempDataExtensionsTests;

public class TryAdd_TryGetValue : TestServiceBase
{
    [Fact]
    public void Adds_And_Gets_Serialized_Value()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var temp = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var obj = new TestObj { Name = "X" };

        // Act
        var added = temp.TryAdd("key", obj, serialize: true);
        var ok = temp.TryGetValue<TestObj>("key", out var result, deserialize: true);

        // Assert
        added.ShouldBeTrue();
        ok.ShouldBeTrue();
        result!.Name.ShouldBe("X");
    }

    private class TestObj
    {
        public string Name { get; set; } = string.Empty;
    }
}