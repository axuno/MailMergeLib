using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YAXLib;

namespace MailMergeLib.Serialization
{
    internal class StringAsCdataSerializer : ICustomSerializer<string>
    {
        public void SerializeToAttribute(string objectToSerialize, XAttribute attrToFill)
        {
            throw new NotImplementedException();

            var several = new Dictionary<string, object>()
            {
                {"Recipient", new Dictionary<string, string>() {{"Email", "sample@example.com"}, {"Name", "John Specimen"}}}
            };
        }

        public void SerializeToElement(string objectToSerialize, XElement elemToFill)
        {
            elemToFill.Add(new XCData(objectToSerialize ?? string.Empty));
        }

        public string SerializeToValue(string objectToSerialize)
        {
            throw new NotImplementedException();
        }

        public string DeserializeFromAttribute(XAttribute attrib)
        {
            throw new NotImplementedException();
        }

        public string DeserializeFromElement(XElement element)
        {
            return element.Value;
        }

        public string DeserializeFromValue(string value)
        {
            throw new NotImplementedException();
        }
    }
}