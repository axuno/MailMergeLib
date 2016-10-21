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
			// build message with a total of 8 placeholders
			var mmm = new MailMergeMessage("Missing in subject {subject}", "Missing in plain text {plain}",
				"<html><body>{html}</body></html>");
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "{from.address}"));
			mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{to.address}"));
			mmm.AddExternalInlineAttachment(new FileAttachment("{inlineAtt.filename}.xml", string.Empty));
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
				Assert.That(exceptions.InnerExceptions.Count == 4);

				foreach (var ex in exceptions.InnerExceptions)
				{
					if (ex is MailMergeMessage.VariableException)
					{
						Assert.That((ex as MailMergeMessage.VariableException).MissingVariable.Count == 8);
						Console.WriteLine($"{nameof(MailMergeMessage.VariableException)} thrown successfully:");
						Console.WriteLine("Missing variables: " + string.Join(", ", (ex as MailMergeMessage.VariableException).MissingVariable));
					}
					if (ex is MailMergeMessage.AddressException)
					{
						Console.WriteLine($"{nameof(MailMergeMessage.AddressException)} thrown successfully:");
						Console.WriteLine((ex as MailMergeMessage.AddressException).Message);
						Assert.That((ex as MailMergeMessage.AddressException).Message == "No recipients." || (ex as MailMergeMessage.AddressException).Message == "No from address.");
					}
					if (ex is MailMergeMessage.AttachmentException)
					{
						Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
						Console.WriteLine("Missing files: " + string.Join(", ", (ex as MailMergeMessage.AttachmentException).BadAttachment));
						Assert.AreEqual(2, (ex as MailMergeMessage.AttachmentException).BadAttachment.Count);
					}
				}
			}
		}
	}
}
