namespace Rsp.IrasPortal.Application.DTOs;
public record ConditionDto
{
    public string Comparison { get; set; } = null!;
    public string OptionsCountOperator { get; set; } = null!;
    public int ParentOptionsCount { get; set; }
    public List<string> ParentOptions { get; set; } = [];
}