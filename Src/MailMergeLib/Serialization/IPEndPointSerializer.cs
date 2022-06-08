using System;
using System.Net;
using System.Xml.Linq;
using YAXLib;

namespace MailMergeLib.Serialization;

/// <summary>
/// Serializer for <see cref="IPEndPoint"/> objects.
/// </summary>
internal class IPEndPointSerializer : ICustomSerializer<IPEndPoint?>
{
    public void SerializeToAttribute(IPEndPoint? objectToSerialize, XAttribute attrToFill)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(IPEndPoint? objectToSerialize, XElement elemToFill)
    {
        if (objectToSerialize == null) return;

        elemToFill.SetAttributeValue("Address", objectToSerialize.Address.ToString());
        elemToFill.SetAttributeValue("Port", objectToSerialize.Port.ToString());
    }

    public string SerializeToValue(IPEndPoint? objectToSerialize)
    {
        throw new NotImplementedException();
    }

    public IPEndPoint DeserializeFromAttribute(XAttribute attrib)
    {
        throw new NotImplementedException();
    }

    public IPEndPoint? DeserializeFromElement(XElement element)
    {
        if (!element.HasAttributes) return null;

        var addr = element.Attribute("Address");
        if (addr == null) return null;

        var port = element.Attribute("Port");
        if (port == null) return null;

        return new IPEndPoint(IPAddress.Parse(addr.Value), int.Parse(port.Value));
    }

    public IPEndPoint DeserializeFromValue(string value)
    {
        throw new NotImplementedException();
    }
}