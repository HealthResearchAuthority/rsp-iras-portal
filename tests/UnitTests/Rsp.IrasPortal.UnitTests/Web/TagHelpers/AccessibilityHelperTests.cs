using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Web.TagHelpers;

namespace Rsp.IrasPortal.UnitTests.Web.TagHelpers;

public class BuildAriaTagHelperTests : TestServiceBase
{
    private static async Task<TagHelperOutput> RunTagHelperAsync(string? describedForValue, string? idForValue)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        var tagHelper = new BuildAriaTagHelper
        {
            DescribedFor = describedForValue != null
                ? new ModelExpression("DescribedProp", new ModelExplorer(metadataProvider, metadata, describedForValue))
                : null!,

            IdFor = idForValue != null
                ? new ModelExpression("IdProp", new ModelExplorer(metadataProvider, metadata, idForValue))
                : null!
        };

        var context = new TagHelperContext(
            tagName: "input",
            allAttributes: new TagHelperAttributeList
            {
                    { "aria-described-for", describedForValue },
                    { "aria-id-for", idForValue }
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "input",
            new TagHelperAttributeList
            {
                    { "aria-described-for", describedForValue },
                    { "aria-id-for", idForValue }
            },
            (useCachedResult, encoder) =>
            {
                var content = new DefaultTagHelperContent();
                content.SetContent("Test content");
                return Task.FromResult<TagHelperContent>(content);
            });

        // Act
        await tagHelper.ProcessAsync(context, output);
        return output;
    }

    [Fact]
    public async Task Sets_AriaDescribedBy_When_DescribedFor_Provided()
    {
        var output = await RunTagHelperAsync("Short Project Title", null);

        // Assert
        Assert.Contains(output.Attributes, a => a.Name == "aria-describedby");
        Assert.Equal("short-project-title-hint", output.Attributes["aria-describedby"].Value);
    }

    [Fact]
    public async Task Sets_Id_When_IdFor_Provided()
    {
        var output = await RunTagHelperAsync(null, "Project Planned End Date");

        // Assert
        Assert.Contains(output.Attributes, a => a.Name == "id");
        Assert.Equal("project-planned-end-date-hint", output.Attributes["id"].Value);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task Does_Not_Set_Attributes_When_Model_Values_Invalid(string? describedFor, string? idFor)
    {
        var output = await RunTagHelperAsync(describedFor, idFor);

        // Assert
        Assert.DoesNotContain(output.Attributes, a => a.Name == "aria-describedby");
        Assert.DoesNotContain(output.Attributes, a => a.Name == "id");
    }
}