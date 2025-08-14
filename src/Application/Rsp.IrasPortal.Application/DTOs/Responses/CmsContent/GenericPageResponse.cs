using System.Text.Json.Serialization;
using Rsp.IrasPortal.Application.JsonConverters;

namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class GenericPageResponse
{
    public string ContentType { get; set; }

    public string Name { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }

    public Route Route { get; set; }

    public Guid Id { get; set; }

    public PropertiesRoot Properties { get; set; }

    public Dictionary<string, object> Cultures { get; set; }
}

public class Route
{
    public string Path { get; set; }

    public StartItem StartItem { get; set; }
}

public class StartItem
{
    public Guid Id { get; set; }

    public string Path { get; set; }
}

public class PropertiesRoot
{
    public bool HasNoContent { get; set; }
    public PageContent PageContent { get; set; }
}

public class PageContent
{
    public List<ComponentItem> Items { get; set; }
}

public class ComponentItem
{
    public ComponentContent Content { get; set; }
    public object Settings { get; set; }
}

public class HeadlineProperties
{
    public string Title { get; set; }
    public string HeadlineType { get; set; }
}

public class RichTextProperties
{
    public RichTextValue Value { get; set; }
}

public class RichTextValue
{
    public string Markup { get; set; }
}

public class BodyTextProperties
{
    public string Value { get; set; }
}

public class DetailsProperties
{
    public string Title { get; set; }

    public string Value { get; set; }
}

public class TabsProperties
{
    public TabItemsCollection Items { get; set; }
}

public class TabItemsCollection
{
    public List<TabItem> Items { get; set; }
}

public class TabItem
{
    public TabItemContent Content { get; set; }
}

public class TabItemContent : BaseContentItem
{
    public TabItemProperties Properties { get; set; }
}

public class TabItemProperties
{
    public string Title { get; set; }
    public string Value { get; set; }
}

public class AccordionProperties
{
    public AccordionItemsCollection Items { get; set; }
}

public class AccordionItemsCollection
{
    public List<AccordionItem> Items { get; set; }
}

public class AccordionItem
{
    public AccordionItemContent Content { get; set; }
}

public class AccordionItemContent : BaseContentItem
{
    public TabItemProperties Properties { get; set; }
}

public class AccordionItemProperties
{
    public string Title { get; set; }
    public string Value { get; set; }
}

[JsonConverter(typeof(ComponentContentConverter))]
public class ComponentContent
{
    public string ContentType { get; set; }
    public Guid Id { get; set; }
    public object Properties { get; set; }
}