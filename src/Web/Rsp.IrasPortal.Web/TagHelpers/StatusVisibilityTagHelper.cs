using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Rsp.Portal.Web.TagHelpers;

/// <summary> Tag helper that conditionally renders (or hides) content based on a record's status.
/// Usage examples:
/// - <div status-for="Model.Status" status-is="Approved">...</div>
/// - <div status-for="Model.Status" status-in="new List<string>{ "A", "B" }" status-mode="Hide">...</div> </summary>
[HtmlTargetElement("*", Attributes = StatusForAttribute)]
[HtmlTargetElement("status-when", Attributes = StatusForAttribute)]
public class StatusVisibilityTagHelper : TagHelper
{
    private const string StatusForAttribute = "status-for";
    private const string SingleStatusAttribute = "status-is";
    private const string StatusInAttribute = "status-in";
    private const string ModeAttribute = "status-mode";

    /// <summary>
    /// Mode that controls the helper behavior:
    /// - Show: default; element is shown only when status matches the provided values.
    /// - Hide: element is hidden when status matches the provided values.
    /// </summary>
    public enum StatusVisibilityMode
    {
        Show,   // default behavior: show only when there's a match
        Hide    // hide when there's a match
    }

    public override int Order => 1;

    /// <summary>
    /// Model expression pointing to the status value to evaluate (e.g. Model.Status). Required for
    /// the helper to function.
    /// </summary>
    [HtmlAttributeName(StatusForAttribute)]
    public ModelExpression StatusFor { get; set; } = null!;

    /// <summary>
    /// Single status to compare against (e.g. status-is="Approved"). Optional. Mutually exclusive
    /// with 'status-in' and 'status-list'.
    /// </summary>
    [HtmlAttributeName(SingleStatusAttribute)]
    public string? SingleStatus { get; set; }

    /// <summary>
    /// A collection of statuses to compare against. Optional. Mutually exclusive with 'status-is'.
    /// </summary>
    [HtmlAttributeName(StatusInAttribute)]
    public IEnumerable<string>? StatusList { get; set; }

    /// <summary>
    /// Controls whether matching statuses should be shown (Show) or hidden (Hide). Default is Show.
    /// </summary>
    [HtmlAttributeName(ModeAttribute)]
    public StatusVisibilityMode Mode { get; set; } = StatusVisibilityMode.Show;

    /// <summary>
    /// Main processing entry point for the TagHelper. Aggregates provided statuses, determines if
    /// the current model status matches, and conditionally suppresses the output according to the
    /// selected mode.
    /// </summary>
    /// <param name="context">Tag helper context (unused).</param>
    /// <param name="output">Tag helper output; may be suppressed to hide content.</param>
    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Ensure callers do not provide conflicting status attributes.
        Validate();

        // Safely read the current status from the bound ModelExpression; the Model may be null.
        var currentStatus = StatusFor?.Model?.ToString();

        // If no status is available, we cannot make a meaningful comparison. Default to keeping the
        // element visible (do not suppress output).
        if (currentStatus == null)
        {
            return Task.CompletedTask;
        }

        // Use a case-insensitive set to allow case-insensitive comparisons of statuses.
        var statuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // If a single status was provided, add it to the set.
        if (!string.IsNullOrWhiteSpace(SingleStatus))
        {
            statuses.Add(SingleStatus);
        }

        // If a collection was provided, add each entry. Null checks already performed.
        if (StatusList != null)
        {
            // Protect against nulls inside the collection.
            foreach (var status in StatusList.Where(status => !string.IsNullOrEmpty(status)))
            {
                statuses.Add(status);
            }
        }

        // Determine whether the current status exists in the aggregated set. Use the HashSet's
        // comparer for case-insensitive matching.
        bool isMatch = statuses.Contains(currentStatus);

        // Decide whether to suppress output based on the selected mode:
        // - Show mode: suppress when there is NOT a match (we only want to show when matching).
        // - Hide mode: suppress when there IS a match (we want to hide matching statuses).
        bool suppressOutput =
            (Mode == StatusVisibilityMode.Show && !isMatch) ||
            (Mode == StatusVisibilityMode.Hide && isMatch);

        if (suppressOutput)
        {
            // Remove the element's output entirely so nothing renders in the final HTML.
            output.SuppressOutput();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates attribute combinations: Exactly zero or one of 'status-is', 'status-in', or
    /// 'status-list' may be provided. If more than one is present an InvalidOperationException is
    /// thrown to avoid ambiguity.
    /// </summary>
    public void Validate()
    {
        // Count how many mutually-exclusive attributes were provided.
        var providedAttributesCount =
            (string.IsNullOrWhiteSpace(SingleStatus) ? 0 : 1) +
            (StatusList?.Any() is true ? 1 : 0);

        if (providedAttributesCount > 1)
        {
            // Provide a clear error message to the developer indicating the conflict.
            throw new InvalidOperationException
            (
                "Only one of 'status-is', 'status-in' can be used at a time."
            );
        }
    }
}