using System;
using System.Linq;
using System.Xml.Linq;
using MimeKit;
using YAXLib;
using YAXLib.Customization;

namespace MailMergeLib.Serialization;

internal class HeaderListSerializer : ICustomSerializer<HeaderList>
{
    private const string HeaderElementName = "Header";
    private const string HeaderIdName = "Id";
    private const string HeaderValueName = "Value";
 
    public void SerializeToAttribute(HeaderList objectToSerialize, XAttribute attrToFill,
        ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(HeaderList objectToSerialize, XElement elemToFill, ISerializationContext serializationContext)
    {
        foreach (var header in objectToSerialize)
        {
            var element = new XElement(HeaderElementName);
            element.SetAttributeValue(HeaderIdName, header.Id.ToString());
            element.SetAttributeValue(HeaderValueName, header.Value);
            elemToFill.Add(element);
        }
    }

    public string SerializeToValue(HeaderList objectToSerialize, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public HeaderList DeserializeFromAttribute(XAttribute attribute, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public HeaderList DeserializeFromElement(XElement element, ISerializationContext serializationContext)
    {
        var hl = new HeaderList();

        foreach (var header in element.Elements(HeaderElementName))
        {
            var idAttr = header.Attributes(HeaderIdName).FirstOrDefault();
            if (idAttr != null)
            {
                if (Enum.TryParse(idAttr.Value, out HeaderId id))
                {
                    var valueAttr = header.Attributes(HeaderValueName).FirstOrDefault();
                    if (valueAttr != null)
                    {
                        hl.Add(id, valueAttr.Value);
                    }
                }
            }
        }
        return hl;
    }

    public HeaderList DeserializeFromValue(string value, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }
}