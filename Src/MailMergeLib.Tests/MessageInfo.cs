﻿using System.Xml;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class MessageInfo
{
    [Test]
    public void ReadAndCompareInfo()
    {
        var info = new MailMergeLib.MessageStore.MessageInfo { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint"};

        var xml = "<MailMergeMessage>" +
                  "<Info>" +
                  $"<Id>{info.Id}</Id>\n" +
                  $"<Category>{info.Category}</Category>" +
                  $"<Description>{info.Description}</Description>\n\r" +
                  $"<Comments>{info.Comments}</Comments>" +
                  $"<Data><![CDATA[{info.Data}]]></Data>" +
                  "</Info>" +
                  "</MailMergeMessage>";

        // MessageInfo
        var deserializedInfo = MailMergeLib.MessageStore.MessageInfoBase.Read(xml);
        Assert.AreEqual(info, deserializedInfo);
        Assert.IsFalse(info.Equals(null));
        Assert.IsTrue(info.Equals(info));
        Assert.IsFalse(info.Equals(new object()));
        Assert.AreEqual(info.GetHashCode(), deserializedInfo.GetHashCode());
    }

    [Test]
    public void ReadAndCompareEmptyInfo()
    {
        var info = new MailMergeLib.MessageStore.MessageInfo { Id = 1, Category = null, Comments = null, Description = null, Data = null };

        var xml = "<MailMergeMessage>" +
                  "<Info>" +
                  $"<Id>{info.Id}</Id>\n" +
                  "</Info>" +
                  "</MailMergeMessage>";

        // MessageInfo
        var deserializedInfo = MailMergeLib.MessageStore.MessageInfoBase.Read(xml);
        Assert.AreEqual(info, deserializedInfo);
        Assert.IsFalse(info.Equals(null));
        Assert.IsTrue(info.Equals(info));
        Assert.IsFalse(info.Equals(new object()));
        Assert.AreEqual(info.GetHashCode(), deserializedInfo.GetHashCode());
    }

    [Test]
    public void ReadInfo_BadElementInsideInfo()
    {
        var info = new MailMergeLib.MessageStore.MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

        var xml = "<MailMergeMessage>" +
                  "<Info>" +
                  $"<BadElement>{info.Id}</BadElement>\n" +
                  "</Info>" +
                  "</MailMergeMessage>";

        Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
    }

    [Test]
    public void ReadInfo_ButInfoElementIsMissing()
    {
        var info = new MailMergeLib.MessageStore.MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

        var xml = "<MailMergeMessage>" +
                  "<InfoMissing>" +
                  $"<Id>{info.Id}</Id>\n" +
                  "</InfoMissing>" +
                  "</MailMergeMessage>";

        Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
    }

    [Test]
    public void ReadInfo_WithTwoInfoElements()
    {
        var info = new MailMergeLib.MessageStore.MessageInfo() { Id = 1, Category = "Some cat.", Comments = "Somme comments", Description = "No description", Data = "Some data hint" };

        var xml = "<MailMergeMessage>" +
                  "<Info>" +
                  $"<Id>{info.Id}</Id>\n" +
                  "</Info>" +
                  "<Info>" +
                  $"<Id>{info.Id}</Id>\n" +
                  "</Info>" + 
                  "</MailMergeMessage>";

        Assert.Throws<XmlException>(() => MailMergeLib.MessageStore.MessageInfoBase.Read(xml));
    }
}