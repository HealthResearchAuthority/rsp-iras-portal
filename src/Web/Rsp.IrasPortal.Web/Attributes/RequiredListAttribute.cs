using System.ComponentModel.DataAnnotations;

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