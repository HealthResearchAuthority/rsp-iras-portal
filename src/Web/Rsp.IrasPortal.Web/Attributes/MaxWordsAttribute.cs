using System.ComponentModel.DataAnnotations;

namespace Rsp.IrasPortal.Web.Attributes;

using System.ComponentModel.DataAnnotations;
using System.Linq;

public class MaxWordsAttribute : ValidationAttribute
{
    private readonly int _maxWords;

    public MaxWordsAttribute(int maxWords, string errorMessage = null)
    {
        _maxWords = maxWords;
        ErrorMessage = errorMessage ?? $"The field can have a maximum of {_maxWords} words.";
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            int wordCount = str.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > _maxWords)
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
}
