using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.ValidationAttributes;

// attribute that will make a property required if the value of another property is
[AttributeUsage(AttributeTargets.Property)]
public class RequiredIfTrueAttribute : ValidationAttribute
{
    private readonly string _conditionalProperty;
    private readonly string _conditionalPropertyValue;

    public RequiredIfTrueAttribute(string conditionalProperty, string conditionalPropertyValue)
    {
        _conditionalProperty = conditionalProperty;
        _conditionalPropertyValue = conditionalPropertyValue;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        // Get the value of the conditional property (the one that must be true)
        var conditionalProperty = validationContext.ObjectType.GetProperty(_conditionalProperty);
        if (conditionalProperty == null)
        {
            return new ValidationResult($"Unknown property: {_conditionalProperty}");
        }

        var conditionalValue = conditionalProperty.GetValue(validationContext.ObjectInstance, null)?.ToString();
        // check if the actial value of the conditional field equals the conditional value
        if (conditionalValue == _conditionalPropertyValue)
        {
            // If the condition is true, validate the current field
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return new ValidationResult($"The field is required when {_conditionalProperty} is true.");
            }
        }

        return ValidationResult.Success!;
    }
}