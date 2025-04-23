using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Web.TagHelpers;

namespace Rsp.IrasPortal.UnitTests.Web.TagHelpers;

public class AccessibilityTagHelperTests : TestServiceBase
{
    [Fact]
    public async Task Sets_AriaDescribedBy_When_DescribedFor_Is_Provided()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        var tagHelper = new AccessibilityTagHelper
        {
            DescribedFor = new ModelExpression("QuestionText", new ModelExplorer(metadataProvider, metadata, "Short project title"))
        };

        var output = new TagHelperOutput("input",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object?>(),
            "test");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var expected = "short-project-title-hint";
        Assert.True(output.Attributes.ContainsName("aria-describedby"));
        Assert.Equal(expected, output.Attributes["aria-describedby"].Value);
    }

    [Fact]
    public async Task Sets_Id_When_IdFor_Is_Provided()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        var tagHelper = new AccessibilityTagHelper
        {
            IdFor = new ModelExpression("QuestionText", new ModelExplorer(metadataProvider, metadata, "Planned end date"))
        };

        var output = new TagHelperOutput("div",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object?>(),
            "test");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var expected = "planned-end-date-hint";
        Assert.True(output.Attributes.ContainsName("id"));
        Assert.Equal(expected, output.Attributes["id"].Value);
    }

    [Fact]
    public async Task Does_Not_Set_Attributes_If_Model_Is_Empty()
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        var tagHelper = new AccessibilityTagHelper
        {
            DescribedFor = new ModelExpression("QuestionText", new ModelExplorer(metadataProvider, metadata, string.Empty))
        };

        var output = new TagHelperOutput("span",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object?>(),
            "test");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.False(output.Attributes.ContainsName("aria-describedby"));
        Assert.False(output.Attributes.ContainsName("id"));
    }
}