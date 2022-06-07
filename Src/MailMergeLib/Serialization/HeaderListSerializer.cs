using System;
using System.Linq;
using System.Xml.Linq;
using MimeKit;
using YAXLib;

namespace MailMergeLib.Serialization;

internal class HeaderListSerializer : ICustomSerializer<HeaderList>
{
    private const string HeaderElementName = "Header";
    private const string HeaderIdName = "Id";
    private const string HeaderValueName = "Value";
    public void SerializeToAttribute(HeaderList objectToSerialize, XAttribute attrToFill)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(HeaderList objectToSerialize, XElement elemToFill)
    {
        foreach (var header in objectToSerialize)
        {
            var element = new XElement(HeaderElementName);
            element.SetAttributeValue(HeaderIdName, header.Id.ToString());
            element.SetAttributeValue(HeaderValueName, header.Value);
            elemToFill.Add(element);
        }
    }

    public string SerializeToValue(HeaderList objectToSerialize)
    {
        throw new NotImplementedException();
    }

    public HeaderList DeserializeFromAttribute(XAttribute attrib)
    {
        throw new NotImplementedException();
    }

    public HeaderList DeserializeFromElement(XElement element)
    {
        var hl = new HeaderList();

        foreach (var header in element.Elements(HeaderElementName))
        {
            HeaderId id;
            var idAttr = header.Attributes(HeaderIdName).FirstOrDefault();
            if (idAttr != null)
            {
                if (Enum.TryParse(idAttr.Value, out id))
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

    public HeaderList DeserializeFromValue(string value)
    {
        throw new NotImplementedException();
    }
}