namespace Rsp.Portal.Application.DTOs;
public record ConditionDto
{
    /// <summary>
    /// Indicates if a condition is applied as an OR or AND
    /// </summary>
    public string Mode { get; set; } = null!;

    /// <summary>
    /// Specifies the type of operation needs to performed
    /// e.g. LENGTH (length check will be performed),
    /// RegEx (Match will be performed using RexEx expression)
    /// IN (lookup into parentOptions)
    /// EQUAL (Equality operation will be performed)
    /// </summary>
    public string Operator { get; set; } = null!;

    /// <summary>
    /// Depends on the Operator.
    /// LENGTH, Value=(Min,Max), RegEx, Value=(Expression)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Indicates if this condition should be negated
    /// </summary>
    public bool Negate { get; set; }

    /// <summary>
    /// List of parentOptions to compare against
    /// </summary>
    public List<string> ParentOptions { get; set; } = [];

    /// <summary>
    /// Type of ParentOptions, single (radio button, boolean) or multiple (checkboxes etc)
    /// </summary>
    public string OptionType { get; set; } = null!;

    /// <summary>
    /// Description of the condition, can be used to display error message for the property
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if the condition is applicable, so we can display
    /// the description as a validation message for the property
    /// </summary>
    public bool IsApplicable { get; set; }
}