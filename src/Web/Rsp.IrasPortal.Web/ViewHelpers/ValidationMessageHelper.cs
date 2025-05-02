using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.ViewHelpers;

/// <summary>
/// Helper class for building validation messages for Razor Views.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ValidationMessageHelper
{
    // CSS class used for validation messages.
    private const string ValidationMessageClass = "govuk-error-message";

    /// <summary>
    /// Builds a validation message for a given error key and validation message.
    /// It also considers applicable conditions from the provided rules to generate additional messages.
    /// </summary>
    /// <param name="html">The HTML helper instance.</param>
    /// <param name="rules">The list of rules to evaluate for applicable conditions.</param>
    /// <param name="errorKey">The key associated with the validation error.</param>
    /// <param name="validationMessage">The default validation message to display if no conditions are applicable.</param>
    /// <returns>An <see cref="IHtmlContent"/> containing the generated validation messages.</returns>
    public static IHtmlContent BuildValidationMessage(this IHtmlHelper html, IList<RuleDto> rules, string errorKey, string validationMessage)
    {
        // Get all applicable conditions from the rules.
        var conditions = rules.SelectMany(rule => rule.GetApplicableConditions());

        // List to track unique validation messages.
        var validationMessages = new List<string>();

        // Builder for constructing the HTML content.
        var contentBuilder = new HtmlContentBuilder();

        // If there are no applicable conditions, use the default validation message.
        if (!conditions.Any())
        {
            // Avoid duplicate messages by checking if the message is already added.
            contentBuilder.AppendHtml(html.ValidationMessage(errorKey, validationMessage, new { @class = ValidationMessageClass }));
            validationMessages.Add(validationMessage);
        }

        // Iterate through the descriptions of applicable conditions.
        foreach (var description in conditions.Select(condition => condition.Description))
        {
            // Add the validation message if it is not already present in the list.
            if (!validationMessages.Contains(description!, StringComparer.OrdinalIgnoreCase))
            {
                contentBuilder.AppendHtml(html.ValidationMessage(errorKey, description, new { @class = ValidationMessageClass }));
                validationMessages.Add(description!);
            }
        }

        // Return the constructed HTML content.
        return contentBuilder;
    }
}