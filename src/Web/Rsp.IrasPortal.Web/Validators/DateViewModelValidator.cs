using FluentValidation;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

/// <summary>
/// Validator for <see cref="DateViewModel"/> that ensures the date is valid and not in the future.
/// Allows for custom validation messages and property names via context data.
/// </summary>
public class DateViewModelValidator : AbstractValidator<DateViewModel>
{
    // Default validation message if not overridden by context
    private string modelValidationMessage = "The date should be a valid date and in future";

    // Default property name if not overridden by context
    private string modelValidationPropertyName = "Date";

    /// <summary>
    /// Configures validation rules for <see cref="DateViewModel"/>.
    /// Only validates if the Date property has a value.
    /// Ensures the date is not in the future.
    /// </summary>
    public DateViewModelValidator()
    {
        RuleFor(model => model.Date)
            .Cascade(CascadeMode.Stop)
            .Custom((date, context) =>
            {
                // date shouldn't be null
                if (!date.HasValue)
                {
                    AddFailures(context);
                }
            })
            .Custom((date, context) =>
            {
                // date must be in future
                if (date!.Value <= DateTime.Now.Date)
                {
                    AddFailures(context);
                }
            });
    }

    private void AddFailures(ValidationContext<DateViewModel> context)
    {
        if (context.RootContextData.TryGetValue(ValidationKeys.ValidationMessage, out object? message) && message is string validationMessage)
        {
            modelValidationMessage = validationMessage;
        }

        if (context.RootContextData.TryGetValue(ValidationKeys.PropertyName, out object? propertyName) && propertyName is string validationProperty)
        {
            modelValidationPropertyName = validationProperty;
        }

        context.AddFailure(modelValidationPropertyName, modelValidationMessage);
    }
}