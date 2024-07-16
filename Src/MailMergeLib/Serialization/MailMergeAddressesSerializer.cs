using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YAXLib;
using YAXLib.Customization;

namespace MailMergeLib.Serialization;

internal class MailMergeAddressesSerializer : ICustomSerializer<MailMergeAddressCollection>
{
    public void SerializeToAttribute(MailMergeAddressCollection objectToSerialize, XAttribute attrToFill, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(MailMergeAddressCollection objectToSerialize, XElement elemToFill, ISerializationContext serializationContext)
    {
        var serializer = SerializationFactory.GetStandardSerializer(typeof(MailMergeAddress));
        foreach (var addr in objectToSerialize)
        {
            elemToFill.Add(serializer.SerializeToXDocument(addr).FirstNode);
        }
    }

    public string SerializeToValue(MailMergeAddressCollection objectToSerialize, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public MailMergeAddressCollection DeserializeFromAttribute(XAttribute attrib, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }

    public MailMergeAddressCollection DeserializeFromElement(XElement element, ISerializationContext serializationContext)
    {
        var addrColl = new MailMergeAddressCollection();
        var serializer = SerializationFactory.GetStandardSerializer(typeof(MailMergeAddressCollection));

        if (serializer.Deserialize(element) is not IEnumerable<MailMergeAddress> result)
            return addrColl;
        
        foreach (var address in result)
        {
            addrColl.AddWithCurrentCharacterEncoding(address);
        }
        return addrColl;
    }

    public MailMergeAddressCollection DeserializeFromValue(string value, ISerializationContext serializationContext)
    {
        throw new NotImplementedException();
    }
}
