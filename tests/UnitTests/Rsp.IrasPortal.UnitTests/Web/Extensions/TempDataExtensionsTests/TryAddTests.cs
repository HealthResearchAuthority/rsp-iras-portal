namespace Rsp.IrasPortal.UnitTests.Web.Extensions.TempDataExtensionsTests;

using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Extensions;
using Shouldly;
using Xunit;

public class TryAddTests : TestServiceBase
{
    [Theory, AutoData]
    public void Should_AddValue_When_SerializeIsFalse(string key, Generator<IrasApplicationResponse> generator)
    {
        // Arrange
        var dictionary = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var value = generator.First();

        // Act
        var result = dictionary.TryAdd(key, value, false);

        // Assert
        result.ShouldBeTrue();
        dictionary[key].ShouldBeEquivalentTo(value);
    }

    [Theory, AutoData]
    public void Should_AddSerializedValue_When_SerializeIsTrue(Guid guid, Generator<IrasApplicationResponse> generator)
    {
        // Arrange
        var key = guid.ToString();

        var dictionary = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        var value = generator.First();

        // Act
        var result = dictionary.TryAdd(key, value, true);

        // Assert
        result.ShouldBeTrue();
        var deserializedValue = JsonSerializer.Deserialize<IrasApplicationResponse>((string)dictionary[key]!);
        deserializedValue.ShouldBeEquivalentTo(value);
    }
}