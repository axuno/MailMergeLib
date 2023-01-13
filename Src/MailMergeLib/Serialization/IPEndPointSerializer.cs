using System;
using System.Net;
using System.Xml.Linq;
using YAXLib;
using YAXLib.Customization;

namespace MailMergeLib.Serialization;

/// <summary>
/// Serializer for <see cref="IPEndPoint"/> objects.
/// </summary>
internal class IPEndPointSerializer : ICustomSerializer<IPEndPoint>
{
    public void SerializeToAttribute(IPEndPoint objectToSerialize, XAttribute attrToFill, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(IPEndPoint objectToSerialize, XElement elemToFill, ISerializationContext serializationContext)
    {
        if (objectToSerialize == null) return;

        elemToFill.SetAttributeValue("Address", objectToSerialize.Address.ToString());
        elemToFill.SetAttributeValue("Port", objectToSerialize.Port.ToString());
    }

    public string SerializeToValue(IPEndPoint objectToSerialize, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public IPEndPoint DeserializeFromAttribute(XAttribute attrib, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public IPEndPoint DeserializeFromElement(XElement element, ISerializationContext serializationContext)
    {
        if (!element.HasAttributes) return null;

        var addr = element.Attribute("Address");
        if (addr == null) return null;

        var port = element.Attribute("Port");
        if (port == null) return null;

        return new IPEndPoint(IPAddress.Parse(addr.Value), int.Parse(port.Value));
    }

    public IPEndPoint DeserializeFromValue(string value, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }
}