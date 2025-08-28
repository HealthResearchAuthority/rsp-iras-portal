namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class LinkModel
{
    public string? Title { get; set; }

    public string? Target { get; set; }

    public string? Url { get; set; }

    public LinkRoute? Route { get; set; }
}

public class LinkRoute
{
    public string? Path { get; set; }
}