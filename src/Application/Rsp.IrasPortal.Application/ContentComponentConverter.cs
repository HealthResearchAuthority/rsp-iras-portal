using System.Text.Json;
using System.Text.Json.Serialization;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

namespace Rsp.IrasPortal.Application;

public class ContentComponentConverter : JsonConverter<ContentComponent>
{
    public override ContentComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var contentType = root.GetProperty("contentType").GetString();

        ContentComponent component = contentType switch
        {
            "accordionComponent" => JsonSerializer.Deserialize<AccordionComponentModel>(root.GetRawText(), options)!,
            "detailsComponent" => JsonSerializer.Deserialize<DetailsContentComponent>(root.GetRawText(), options)!,
            "tabsComponent" => JsonSerializer.Deserialize<TabsComponentModel>(root.GetRawText(), options)!,
            "bodyTextComponent" => JsonSerializer.Deserialize<BodyTextComponentModel>(root.GetRawText(), options)!,
            _ => throw new JsonException($"Unknown contentType: {contentType}")
        };

        return component;
    }

    public override void Write(Utf8JsonWriter writer, ContentComponent value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}