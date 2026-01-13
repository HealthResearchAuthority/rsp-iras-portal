using System.Text.Json.Serialization;
using Rsp.Portal.Application.JsonConverters;

namespace Rsp.Portal.Application.DTOs.Responses.CmsContent;

[JsonConverter(typeof(ComponentContentConverter))]
public class ComponentContent
{
    public string? ContentType { get; set; }
    public Guid Id { get; set; }
    public object? Properties { get; set; }
}