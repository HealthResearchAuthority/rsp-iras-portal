using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers;

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
        output.Attributes.ShouldContain(a => a.Name == "aria-describedby");
        output.Attributes["aria-describedby"].Value.ShouldBe("short-project-title-hint");
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
        output.Attributes.ShouldContain(a => a.Name == "id");
        output.Attributes["id"].Value.ShouldBe("planned-end-date-hint");
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
        output.Attributes.ShouldNotContain(a => a.Name == "aria-describedby");
        output.Attributes.ShouldNotContain(a => a.Name == "id");
    }
}