using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Rsp.IrasPortal.Web.Extensions;

/// <summary>
/// Extension methods for ModelStateDictionary to support conversion to dictionary and restoration from TempData.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ModelStateExtensions
{
    /// <summary>
    /// Converts the ModelStateDictionary to a dictionary containing only the keys with errors and their first error message.
    /// </summary>
    /// <param name="modelState">The ModelStateDictionary instance.</param>
    /// <returns>A dictionary with the field names as keys and the first error message as values.</returns>
    public static Dictionary<string, string?> ToDictionary(this ModelStateDictionary modelState)
    {
        return modelState
            .Where(x => x.Value?.Errors.Count > 0) // Only include entries with errors
            .ToDictionary
            (
                m => m.Key, // Field name
                m => m.Value?.Errors
                    .Select(s => s.ErrorMessage)
                    .FirstOrDefault(s => s != null) // First non-null error message
            );
    }

    /// <summary>
    /// Restores model state errors from TempData into the ModelStateDictionary.
    /// </summary>
    /// <param name="modelState">The ModelStateDictionary instance.</param>
    /// <param name="tempData">The TempData dictionary containing serialized model state.</param>
    /// <param name="key">The key under which the model state dictionary is stored in TempData.</param>
    public static void FromTempData(this ModelStateDictionary modelState, ITempDataDictionary tempData, string key)
    {
        // Try to get the dictionary of errors from TempData
        if (!tempData.TryGetValue<Dictionary<string, string?>>(key, out var stateDictionary, true))
        {
            return;
        }

        if (stateDictionary == null)
        {
            return;
        }

        // Add each error back into the ModelStateDictionary
        foreach (var item in stateDictionary)
        {
            modelState.AddModelError(item.Key, item.Value ?? "");
        }
    }
}