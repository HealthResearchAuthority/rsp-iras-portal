namespace Rsp.IrasPortal.UnitTests.TempDataExtensionsTests;

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

public class TryGetValue : TestServiceBase
{
    [Theory, AutoData]
    public void TryGetValue_Should_ReturnFalse_When_KeyDoesNotExist(string key)
    {
        // Arrange
        var dictionary = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            ["key"] = "value"
        };

        // Act
        var result = dictionary.TryGetValue<object>(key, out var value, false);

        // Assert
        result.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Theory, AutoData]
    public void Should_ReturnDeserializedValue_When_DeserializeIsTrue(string key, Generator<IrasApplicationResponse> generator)
    {
        // Arrange
        var value = generator.First();
        var dictionary = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [key] = JsonSerializer.Serialize(value)
        };

        // Act
        var result = dictionary.TryGetValue<IrasApplicationResponse>(key, out var resultValue, true);

        // Assert
        result.ShouldBeTrue();
        resultValue.ShouldBeEquivalentTo(value);
    }

    [Theory, AutoData]
    public void Should_ReturnValue_When_DeserializeIsFalse(string key, Generator<IrasApplicationResponse> generator)
    {
        // Arrange
        var value = generator.First();
        var dictionary = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [key] = value
        };

        // Act
        var result = dictionary.TryGetValue<IrasApplicationResponse>(key, out var resultValue, false);

        // Assert
        result.ShouldBeTrue();
        resultValue.ShouldBeEquivalentTo(value);
    }
}