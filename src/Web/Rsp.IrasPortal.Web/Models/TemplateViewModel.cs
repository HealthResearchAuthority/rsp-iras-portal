namespace Rsp.IrasPortal.Web.Models;

public class TemplateViewModel
{
    /// <summary>
    /// The unique identifier or an id prefrix for the element.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The name of the element or name prefix, typically used for form submission.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The name of the element actually used for submission by combining
    /// values from multiple inputs. In that case <see cref="Name"/> will be differnt for
    /// each input element.
    /// </summary>
    public string FieldName { get; set; } = null!;

    /// <summary>
    /// The css class to be applied to the element.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Multiple values for the element. This is used for checkboxes and radio buttons.
    /// </summary>
    public List<(string Value, int Index)> Items = new();
}