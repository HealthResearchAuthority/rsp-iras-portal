using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers.ErrorClassTagHelperTests;

public class ErrorClassTagHelperTests : TestServiceBase
{
    [Fact]
    public async Task Should_Not_Add_Error_Class_When_No_ModelState_ErrorsAsync()
    {
        // Arrange
        var tagHelper = CreateTagHelper();

        var context = new TagHelperContext
        (
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var attribute = output.Attributes["class"];

        attribute.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Add_ErrorClass_To_Non_Input_Element_When_ModelState_Has_ErrorsAsync()
    {
        // Arrange
        var tagHelper = CreateTagHelper();

        tagHelper.ViewContext.ViewData.ModelState.AddModelError("TestProperty", "Error message");

        var context = new TagHelperContext
        (
            tagName: "div",
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "div",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var attribute = output.Attributes["class"];

        attribute.ShouldNotBeNull();
        attribute.Value
            .ShouldBeOfType<string>()
            .ShouldContain(tagHelper.ErrorClass);
    }

    [Fact]
    public async Task Should_Add_InputErrorClass_To_Input_Element_When_ModelState_Has_ErrorsAsync()
    {
        // Arrange
        var tagHelper = CreateTagHelper();

        tagHelper.ViewContext.ViewData.ModelState.AddModelError("TestProperty", "Error message");

        var context = new TagHelperContext
        (
            tagName: "input",
            [],
            new Dictionary<object, object>(),
            "test"
        );

        var output = new TagHelperOutput
        (
            "input",
            [],
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var attribute = output.Attributes["class"];

        attribute.ShouldNotBeNull();
        attribute.Value
            .ShouldBeOfType<string>()
            .ShouldContain(tagHelper.InputErrorClass);
    }

    private static ErrorClassTagHelper CreateTagHelper()
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        var viewContext = new ViewContext
        {
            ViewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        };

        return new ErrorClassTagHelper
        {
            ViewContext = viewContext,
            For = new ModelExpression("TestProperty", new ModelExplorer(metadataProvider, metadata, null))
        };
    }
}