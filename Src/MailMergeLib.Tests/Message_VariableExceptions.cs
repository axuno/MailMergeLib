﻿using System;
using System.Linq;
using MailMergeLib.Templates;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
class Message_VariableExceptions
{
    [Test]
    public void MissingVariableAndAttachmentsExceptions()
    {
        // build message with a total of 9 placeholders which will be missing
        var mmm = new MailMergeMessage("Missing in subject {subject}", "Missing in plain text {plain}",
            "<html><head></head><body>{html}{:template(Missing)}</body></html>");
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{from.address}"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{to.address}"));
        mmm.AddExternalInlineAttachment(new FileAttachment("{inlineAtt.filename}.jpg", string.Empty));
        mmm.FileAttachments.Add(new FileAttachment("{fileAtt.filename}.xml", "{fileAtt.displayname}"));

        // **************** Part 1:
        mmm.Config.IgnoreMissingInlineAttachments = false;
        mmm.Config.IgnoreMissingFileAttachments = false;
        // ************************

        try
        {
            mmm.GetMimeMessage(default(object));
            Assert.Fail("Expected exceptions not thrown.");
        }
        catch (MailMergeMessage.MailMergeMessageException exceptions)
        {
            Console.WriteLine($"Aggregate {nameof(MailMergeMessage.MailMergeMessageException)} thrown.");
            Console.WriteLine();
            /* Expected exceptions:
             * 1) 9 missing variables for {placeholders} and {:templates(...)}
             * 2) No recipients
             * 3) No FROM address
             * 4) Missing file attachment {fileAtt.filename}.xml
             * 5) Missing inline attachment {inlineAtt.filename}.jpg
             */
            Assert.That(exceptions.InnerExceptions, Has.Count.EqualTo(5));

            foreach (var ex in exceptions.InnerExceptions.Where(ex => !(ex is MailMergeMessage
                         .AttachmentException)))
            {
                if (ex is MailMergeMessage.VariableException exception)
                {
                    Assert.That(exception.MissingVariable, Has.Count.EqualTo(9));
                    Console.WriteLine($"{nameof(MailMergeMessage.VariableException)} thrown successfully:");
                    Console.WriteLine("Missing variables: " +
                                      string.Join(", ",
                                          exception.MissingVariable));
                    Console.WriteLine();
                }
                if (ex is MailMergeMessage.AddressException addressException)
                {
                    Console.WriteLine($"{nameof(MailMergeMessage.AddressException)} thrown successfully:");
                    Console.WriteLine(addressException.Message);
                    Console.WriteLine();
                    Assert.That(addressException.Message == "No recipients." ||
                                addressException.Message == "No from address.");
                }
            }

            // one exception for a missing file attachment, one for a missing inline attachment
            var attExceptions = exceptions.InnerExceptions.Where(ex => ex is MailMergeMessage.AttachmentException)
                .ToList();
            Assert.That(attExceptions, Has.Count.EqualTo(2));
            foreach (var ex in attExceptions)
            {
                Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
                Console.WriteLine("Missing files: " +
                                  string.Join(", ", (ex as MailMergeMessage.AttachmentException)?.BadAttachment!));
                Console.WriteLine();
                Assert.That((ex as MailMergeMessage.AttachmentException)?.BadAttachment.Count, Is.EqualTo(1));
            }
        }

        // **************** Part 2:
        mmm.Config.IgnoreMissingInlineAttachments = true;
        mmm.Config.IgnoreMissingFileAttachments = true;
        // ************************

        try
        {
            mmm.GetMimeMessage(default(object));
            Assert.Fail("Expected exceptions not thrown.");
        }
        catch (MailMergeMessage.MailMergeMessageException exceptions)
        {
            /* Expected exceptions:
             * 1) 9 missing variables for {placeholders} and {:templates(...)}
             * 2) No recipients
             * 3) No FROM address
             */
            Assert.That(exceptions.InnerExceptions, Has.Count.EqualTo(3));
            Assert.That(exceptions.InnerExceptions.Any(e => e is MailMergeMessage.AttachmentException), Is.False);

            Console.WriteLine("Exceptions for missing attachment files suppressed.");
        }
    }

    [Test]
    public void Template()
    {
        var mmm = MessageFactory.GetHtmlAndPlainMessage_WithTemplates(out var variables);

        var msg = mmm.GetMimeMessage(variables);

        Assert.Multiple(() =>
        {
            Assert.That(msg.Subject, Is.EqualTo(mmm.Subject.Replace("{FirstName}", variables["FirstName"])));

            // DefaultKey is "Formal"
            Assert.That(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Formal"]
                .First(t => t.Type == PartType.Html)
                .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
            Assert.That(msg.TextBody.Contains(mmm.Templates["Salutation"]["Formal"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
        });

        // Programmacically set the part to use
        mmm.Templates[0].Key = "Dear";
        msg = mmm.GetMimeMessage(variables);
        Assert.Multiple(() =>
        {
            Assert.That(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Dear"]
                    .First(t => t.Type == PartType.Html)
                    .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
            Assert.That(msg.TextBody.Contains(mmm.Templates["Salutation"]["Dear"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
        });

        // Neither DefaultKey nore Key of the template are set: gets the first part
        mmm.Templates[0].DefaultKey = null;
        mmm.Templates[0].Key = null;
        // Remove so that only max. 2 parts for 1 key are left
        mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[5]);
        mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[4]);
        mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[3]);
        mmm.Templates[0].Text.Remove(mmm.Templates[0].Text[2]);

        msg = mmm.GetMimeMessage(variables);
        Assert.Multiple(() =>
        {
            Assert.That(msg.HtmlBody.Contains(mmm.Templates["Salutation"]["Hi"]
                    .First(t => t.Type == PartType.Html)
                    .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
            Assert.That(msg.TextBody.Contains(mmm.Templates["Salutation"]["Hi"]
                .First(t => t.Type == PartType.Plain)
                .Value.Replace("{FirstName}", variables["FirstName"])), Is.True);
        });
    }
}