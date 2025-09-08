using System.Text.Json;
using System.Text.Json.Serialization;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

namespace Rsp.IrasPortal.Application.JsonConverters;

public class ComponentContentConverter : JsonConverter<ComponentContent>
{
    public override ComponentContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var contentType = root.GetProperty("contentType").GetString();
        var id = root.GetProperty("id").GetGuid();

        var propertiesElement = root.GetProperty("properties");

        object properties = contentType switch
        {
            "hintTextComponent" => JsonSerializer.Deserialize<HintTextProperties>(propertiesElement.GetRawText(), options),
            "lineSeparatorComponent" => JsonSerializer.Deserialize<LineSeparatorProperties>(propertiesElement.GetRawText(), options),
            "richTextComponent" => JsonSerializer.Deserialize<RichTextProperties>(propertiesElement.GetRawText(), options),
            "headlineComponent" => JsonSerializer.Deserialize<HeadlineProperties>(propertiesElement.GetRawText(), options),
            "bodyTextComponent" => JsonSerializer.Deserialize<BodyTextProperties>(propertiesElement.GetRawText(), options),
            "detailsComponent" => JsonSerializer.Deserialize<DetailsProperties>(propertiesElement.GetRawText(), options),
            "tabsComponent" => JsonSerializer.Deserialize<TabsProperties>(propertiesElement.GetRawText(), options),
            "tabItem" => JsonSerializer.Deserialize<TabItemProperties>(propertiesElement.GetRawText(), options),
            "accordionComponent" => JsonSerializer.Deserialize<AccordionProperties>(propertiesElement.GetRawText(), options),
            "accordionItem" => JsonSerializer.Deserialize<AccordionItemProperties>(propertiesElement.GetRawText(), options),
            _ => JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesElement.GetRawText(), options)
        };

        return new ComponentContent
        {
            ContentType = contentType,
            Id = id,
            Properties = properties
        };
    }

    public override void Write(Utf8JsonWriter writer, ComponentContent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("contentType", value.ContentType);
        writer.WriteString("id", value.Id);

        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.Properties, value.Properties.GetType(), options);

        writer.WriteEndObject();
    }
}