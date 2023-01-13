using System;
using System.Xml.Linq;
using MailMergeLib.Templates;
using YAXLib;
using YAXLib.Customization;
using YAXLib.Exceptions;

namespace MailMergeLib.Serialization;

internal class PartSerializer : ICustomSerializer<Part>
{
    public void SerializeToAttribute(Part objectToSerialize, XAttribute attrToFill, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(Part objectToSerialize, XElement elemToFill, ISerializationContext serializationContext)
    {
        elemToFill.Add(new XAttribute(nameof(objectToSerialize.Key), objectToSerialize.Key));
        elemToFill.Add(new XAttribute(nameof(objectToSerialize.Type), objectToSerialize.Type));
        elemToFill.Add(new XCData(objectToSerialize.Value));
    }

    public string SerializeToValue(Part objectToSerialize, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public Part DeserializeFromAttribute(XAttribute attrib, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public Part DeserializeFromElement(XElement element, ISerializationContext serializationContext)
    {
        Part part;

        var typeAttr = element.Attribute(nameof(part.Type));
        if (typeAttr == null)
        {
            throw new YAXAttributeMissingException(nameof(part.Type));
        }
        var type = (PartType)Enum.Parse(typeof(PartType), typeAttr.Value);
            
        var keyAttr = element.Attribute(nameof(part.Key));
        if (keyAttr == null)
        {
            throw new YAXAttributeMissingException(nameof(part.Key));
        }
            
        return new Part(type, keyAttr.Value, element.Value);
    }

    public Part DeserializeFromValue(string value, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }
}