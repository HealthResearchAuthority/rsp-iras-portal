using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.Attributes;

/// <summary>
/// For checkboxes to ensure that at least one option is selected
/// </summary>
public class RequiredListAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var list = value as List<string>;
        if (list == null || !list.Any())
        {
            return new ValidationResult(ErrorMessage ?? "Select at least one country.");
        }

        return ValidationResult.Success;
    }
}