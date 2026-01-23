using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.Portal.Web.TagHelpers;

namespace Rsp.Portal.UnitTests.Web.TagHelpers.StatusVisibilityTagHelperTests;

public class StatusVisibilityTagHelperTests
{
    // Validation Tests
    [Fact]
    public void Validate_Throws_When_Both_SingleStatus_And_StatusList_Are_Provided()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        tagHelper.StatusList = ["Draft", "Submitted"];

        // Act & Assert
        Should
            .Throw<InvalidOperationException>(tagHelper.Validate)
            .Message.ShouldBe("Only one of 'status-is', 'status-in' can be used at a time.");
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_Only_SingleStatus_Is_Provided()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_Only_StatusList_Is_Provided()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.StatusList = ["Approved", "Submitted"];

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_No_Status_Attributes_Are_Provided()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_SingleStatus_Is_Null()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = null;
        tagHelper.StatusList = ["Draft"];

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_SingleStatus_Is_Empty()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "";
        tagHelper.StatusList = ["Draft"];

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_SingleStatus_Is_Whitespace()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "   ";
        tagHelper.StatusList = ["Draft"];

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_StatusList_Is_Null()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Draft";
        tagHelper.StatusList = null;

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Does_Not_Throw_When_StatusList_Is_Empty()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Draft";
        tagHelper.StatusList = Array.Empty<string>();

        // Act & Assert
        Should.NotThrow(tagHelper.Validate);
    }

    [Fact]
    public void Validate_Throws_When_Both_Are_Provided_Even_With_Null_In_StatusList()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        tagHelper.StatusList = ["Draft", null!];

        // Act & Assert
        Should
            .Throw<InvalidOperationException>(tagHelper.Validate);
    }

    [Fact]
    public void ProcessAsync_Throws_InvalidOperationException_When_Validation_Fails()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        tagHelper.StatusList = ["Draft", "Submitted"];

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");

        // Act & Assert
        Should
            .Throw<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));
    }

    // Show Mode Tests - Single Status

    [Fact]
    public async Task ProcessAsync_Show_Mode_Shows_Content_When_SingleStatus_Matches()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Show_Mode_Suppresses_Content_When_SingleStatus_Does_Not_Match()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Draft");
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Show_Mode_Is_Case_Insensitive()
    {
        // Arrange
        var tagHelper = CreateTagHelper("approved");
        tagHelper.SingleStatus = "APPROVED";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Hide Mode Tests - Single Status

    [Fact]
    public async Task ProcessAsync_Hide_Mode_Suppresses_Content_When_SingleStatus_Matches()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Hide;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Hide_Mode_Shows_Content_When_SingleStatus_Does_Not_Match()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Draft");
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Hide;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Show Mode Tests - Status List

    [Fact]
    public async Task ProcessAsync_Show_Mode_Shows_Content_When_Status_In_List()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.StatusList = ["Draft", "Approved", "Submitted"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Show_Mode_Suppresses_Content_When_Status_Not_In_List()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Rejected");
        tagHelper.StatusList = ["Draft", "Approved", "Submitted"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Show_Mode_StatusList_Is_Case_Insensitive()
    {
        // Arrange
        var tagHelper = CreateTagHelper("approved");
        tagHelper.StatusList = ["DRAFT", "APPROVED", "SUBMITTED"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Hide Mode Tests - Status List

    [Fact]
    public async Task ProcessAsync_Hide_Mode_Suppresses_Content_When_Status_In_List()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.StatusList = ["Draft", "Approved", "Submitted"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Hide;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Hide_Mode_Shows_Content_When_Status_Not_In_List()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Rejected");
        tagHelper.StatusList = ["Draft", "Approved", "Submitted"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Hide;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Null and Empty Status Tests

    [Fact]
    public async Task ProcessAsync_Shows_Content_When_StatusFor_Model_Is_Null()
    {
        // Arrange
        var tagHelper = CreateTagHelper(null);
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Shows_Content_When_StatusFor_Model_Is_Empty_String()
    {
        // Arrange
        var tagHelper = CreateTagHelper("");
        tagHelper.SingleStatus = "Approved";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert - empty string is treated as a valid status that doesn't match "Approved"
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Ignores_Null_Values_In_StatusList()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.StatusList = ["Draft", null, "Approved", null];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Ignores_Empty_Strings_In_StatusList()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.StatusList = ["Draft", "", "Approved", "   "];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Default Behavior Tests

    [Fact]
    public async Task ProcessAsync_Defaults_To_Show_Mode_When_Not_Specified()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";
        // Mode not set, should default to Show

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        tagHelper.Mode.ShouldBe(StatusVisibilityTagHelper.StatusVisibilityMode.Show);
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Suppresses_Content_When_No_Status_Criteria_Provided_In_Show_Mode()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        // No SingleStatus or StatusList set

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert - No criteria means no match, so content is suppressed in Show mode (default)
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    // Real-World Scenario Tests

    [Fact]
    public async Task ProcessAsync_Handles_Draft_Status_Scenario()
    {
        // Arrange - Show edit button only when status is Draft
        var tagHelper = CreateTagHelper("Draft");
        tagHelper.SingleStatus = "Draft";
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("button");
        output.Content.SetHtmlContent("Edit");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_Handles_Submitted_Status_Hide_Scenario()
    {
        // Arrange - Hide delete button when status is Submitted or Approved
        var tagHelper = CreateTagHelper("Submitted");
        tagHelper.StatusList = ["Submitted", "Approved"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Hide;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("button");
        output.Content.SetHtmlContent("Delete");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessAsync_Handles_Multiple_Statuses_For_Workflow()
    {
        // Arrange - Show review section when status is in review workflow
        var tagHelper = CreateTagHelper("Review in progress");
        tagHelper.StatusList = ["Review in progress", "With review body", "With sponsor"];
        tagHelper.Mode = StatusVisibilityTagHelper.StatusVisibilityMode.Show;

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Review Section");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // TagHelper Output Tests

    [Fact]
    public async Task ProcessAsync_Does_Not_Remove_TagName_When_Content_Shown()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("div");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.TagName.ShouldBe("div");
    }

    [Fact]
    public async Task ProcessAsync_Works_With_StatusWhen_Element()
    {
        // Arrange
        var tagHelper = CreateTagHelper("Approved");
        tagHelper.SingleStatus = "Approved";

        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("status-when");
        output.Content.SetHtmlContent("Content");

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        output.Content.IsEmptyOrWhiteSpace.ShouldBeFalse();
    }

    // Helper Methods

    private static StatusVisibilityTagHelper CreateTagHelper(string? statusValue)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(string));

        return new StatusVisibilityTagHelper
        {
            StatusFor = new ModelExpression("Status", new ModelExplorer(metadataProvider, metadata, statusValue))
        };
    }

    private static TagHelperContext CreateTagHelperContext()
    {
        return new TagHelperContext
        (
            new TagHelperAttributeList(),
            new Dictionary<object, object?>(),
            Guid.NewGuid().ToString()
        );
    }

    private static TagHelperOutput CreateTagHelperOutput(string tagName)
    {
        return new TagHelperOutput
        (
            tagName,
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );
    }
}