using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using MailMergeLib;

namespace UnitTests
{
    [TestFixture]
    class MailMergeMessageTests
    {
        [Test]
        public void MissingVariableAndAttachmentsExceptions()
        {
            // build message with a total of 8 placeholders which will be missing
            var mmm = new MailMergeMessage("Missing in subject {subject}", "Missing in plain text {plain}",
                "<html><head></head><body>{html}</body></html>");
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{from.address}"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{to.address}"));
            mmm.AddExternalInlineAttachment(new FileAttachment("{inlineAtt.filename}.jpg", string.Empty));
            mmm.FileAttachments.Add(new FileAttachment("{fileAtt.filename}.xml", "{fileAtt.displayname}"));

            try
            {
                var mimeMessage = mmm.GetMimeMessage(default(object));
                Assert.Fail("Expected exceptions not thrown.");
            }
            catch (MailMergeMessage.MailMergeMessageException exceptions)
            {
                Console.WriteLine($"Aggregate {nameof(MailMergeMessage.MailMergeMessageException)} thrown. Passed.");
                Console.WriteLine();
                /* Expected exceptions:
                 * 1) 8 missing variables for {placeholders}
                 * 2) No recipients
                 * 3) No FROM address
                 * 4) Missing file attachment {fileAtt.filename}.xml
                 * 5) Missing inline attachment {inlineAtt.filename}.jpg
                 */
                Assert.That(exceptions.InnerExceptions.Count == 5);

                foreach (var ex in exceptions.InnerExceptions.Where(ex => !(ex is MailMergeMessage.AttachmentException)))
                {
                    if (ex is MailMergeMessage.VariableException)
                    {
                        Assert.AreEqual(8, (ex as MailMergeMessage.VariableException).MissingVariable.Count);
                        Console.WriteLine($"{nameof(MailMergeMessage.VariableException)} thrown successfully:");
                        Console.WriteLine("Missing variables: " +
                                          string.Join(", ", (ex as MailMergeMessage.VariableException).MissingVariable));
                    }
                    if (ex is MailMergeMessage.AddressException)
                    {
                        Console.WriteLine($"{nameof(MailMergeMessage.AddressException)} thrown successfully:");
                        Console.WriteLine((ex as MailMergeMessage.AddressException).Message);
                        Assert.That((ex as MailMergeMessage.AddressException).Message == "No recipients." ||
                                    (ex as MailMergeMessage.AddressException).Message == "No from address.");
                    }
                }

                // one exception for a missing file attachment, one for a missing inline attachment
                var attExceptions = exceptions.InnerExceptions.Where(ex => ex is MailMergeMessage.AttachmentException).ToList();
                Assert.AreEqual(2, attExceptions.Count);
                foreach (var ex in attExceptions)
                {
                    Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
                    Console.WriteLine("Missing files: " + string.Join(", ", (ex as MailMergeMessage.AttachmentException).BadAttachment));
                    Assert.AreEqual(1, (ex as MailMergeMessage.AttachmentException).BadAttachment.Count);
                }
            }
        }
    }
}
