using System.Text.Json.Serialization;
using Rsp.IrasPortal.Application.JsonConverters;

namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

[JsonConverter(typeof(ComponentContentConverter))]
public class ComponentContent
{
    public string ContentType { get; set; }
    public Guid Id { get; set; }
    public object Properties { get; set; }
}