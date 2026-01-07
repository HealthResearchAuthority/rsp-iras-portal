namespace Rsp.IrasPortal.Web.Models;

public class WorkspaceCardModel
{
    public string? Permission { get; set; }
    public string? Title { get; set; }
    public string? Desc { get; set; }
    public string? Link { get; set; }
    public Dictionary<string, string> Dictionary { get; set; } = new();
}