using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YAXLib;

namespace MailMergeLib.Serialization;

internal class MailMergeAddressesSerializer : ICustomSerializer<MailMergeAddressCollection>
{
    public void SerializeToAttribute(MailMergeAddressCollection objectToSerialize, XAttribute attrToFill)
    {
        throw new NotImplementedException();
    }

    public void SerializeToElement(MailMergeAddressCollection objectToSerialize, XElement elemToFill)
    {
        var serializer = SerializationFactory.GetStandardSerializer(typeof(MailMergeAddress));
        foreach (var addr in objectToSerialize)
        {
            elemToFill.Add(serializer.SerializeToXDocument(addr).FirstNode);
        }
    }

    public string SerializeToValue(MailMergeAddressCollection objectToSerialize)
    {
        throw new NotImplementedException();
    }

    public MailMergeAddressCollection DeserializeFromAttribute(XAttribute attrib)
    {
        throw new NotImplementedException();
    }

    public MailMergeAddressCollection DeserializeFromElement(XElement element)
    {
        var addrColl = new MailMergeAddressCollection();
        var serializer = SerializationFactory.GetStandardSerializer(typeof(MailMergeAddressCollection));
        var result = serializer.Deserialize(element) as List<object>;
        if (result == null) return addrColl;
        foreach (MailMergeAddress addr in result)
        {
            addrColl.AddWithCurrentCharacterEncoding(addr);
        }
        return addrColl;
    }

    public MailMergeAddressCollection DeserializeFromValue(string value)
    {
        throw new NotImplementedException();
    }
}