using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers.CssClassTagHelpersTests;

public class ProcessAsyncTests : TestServiceBase
{
    [Fact]
    public async Task ProcessAsync_Should_Add_ConditionalClass_When_Conditional_Is_True()
    {
        // Arrange
        var tagHelper = new CssClassTagHelper
        {
            Conditional = true,
            ConditionalClass = "test-class"
        };

        var tagHelperContext = new TagHelperContext
        (
            tagName: "div",
            allAttributes: [],
            items: new Dictionary<object, object>(),
            uniqueId: "test"
        );

        var tagHelperOutput = new TagHelperOutput
        (
            tagName: "div",
            attributes: [],
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        // Assert
        var attribute = tagHelperOutput.Attributes["class"];

        attribute.ShouldNotBeNull();
        attribute.Value
            .ShouldBeOfType<string>()
            .ShouldContain("test-class");
    }

    [Fact]
    public async Task ProcessAsync_Should_Not_Add_ConditionalClass_When_Conditional_Is_False()
    {
        // Arrange
        var tagHelper = new CssClassTagHelper
        {
            Conditional = false,
            ConditionalClass = "test-class"
        };

        var tagHelperContext = new TagHelperContext
        (
            tagName: "div",
            allAttributes: [],
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var tagHelperOutput = new TagHelperOutput
        (
            tagName: "div",
            attributes: [],
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        // Assert
        var attribute = tagHelperOutput.Attributes["class"];

        attribute.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessAsync_Should_Handle_Multiple_Classes()
    {
        // Arrange
        var tagHelper = new CssClassTagHelper
        {
            Conditional = true,
            ConditionalClass = "class-one class-two"
        };

        var tagHelperContext = new TagHelperContext
        (
            tagName: "div",
            allAttributes: [],
            items: new Dictionary<object, object>(),
            uniqueId: "test"
        );

        var tagHelperOutput = new TagHelperOutput
        (
            tagName: "div",
            attributes: [],
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        // Assert
        var attribute = tagHelperOutput.Attributes["class"];

        attribute.ShouldNotBeNull();
        var value = attribute.Value.ShouldBeOfType<HtmlString>();

        value.ToString().ShouldContain("class-one");
        value.ToString().ShouldContain("class-two");
    }
}