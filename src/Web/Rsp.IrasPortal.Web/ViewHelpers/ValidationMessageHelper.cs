using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.ViewHelpers;

public static class ValidationMessageHelper
{
    private const string ValidationMessageClass = "govuk-error-message";

    public static IHtmlContent BuildValidationMessage(this IHtmlHelper html, IList<RuleDto> rules, string errorKey, string validationMessage)
    {
        // get all the conditions for the rule
        var conditions = rules.SelectMany(rule => rule.GetApplicableConditions());

        var validationMessages = new List<string>();

        var contentBuilder = new HtmlContentBuilder();

        // if there are not conditions
        // use the validationMessage
        if (!conditions.Any())
        {
            // if the same validationMessage is not present in the list
            // build the validation message and add it, so that it doesn't
            // get repeated
            contentBuilder.AppendHtml(html.ValidationMessage(errorKey, validationMessage, new { @class = ValidationMessageClass }));

            validationMessages.Add(validationMessage);
        }

        foreach (var description in conditions.Select(condition => condition.Description))
        {
            // if the same validationMessage is not present in the list
            // build the validation message and add it, so that it doesn't
            // get repeated
            if (!validationMessages.Contains(description!, StringComparer.OrdinalIgnoreCase))
            {
                contentBuilder.AppendHtml(html.ValidationMessage(errorKey, description, new { @class = ValidationMessageClass })); // = "govuk-error-message"

                validationMessages.Add(description!);
            }
        }

        return contentBuilder;
    }
}