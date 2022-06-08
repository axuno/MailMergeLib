using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SmartFormat;

namespace MailMergeLib.Tests;

[TestFixture]
class Message_Variables
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
        mmm.FileAttachments.Add(new FileAttachment("{throwParingError.xml", "DisplayName"));

        // **************** Part 1:
        mmm.Config.IgnoreMissingInlineAttachments = false;
        mmm.Config.IgnoreMissingFileAttachments = false;
        // ************************

        try
        {
            mmm.GetMimeMessage(default);
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
             * 6) Parsing error in file attachment "{throwParingError.xml"
             */
            Assert.AreEqual(6, exceptions.InnerExceptions.Count);

            foreach (var ex in exceptions.InnerExceptions.Where(ex => ex is not MailMergeMessage.AttachmentException))
            {
                if (ex is MailMergeMessage.VariableException variableException)
                {
                    Assert.AreEqual(9, variableException.MissingVariable.Count);
                    Console.WriteLine($"{nameof(MailMergeMessage.VariableException)} thrown successfully:");
                    Console.WriteLine("Missing variables: " +
                                      string.Join(", ",
                                          variableException.MissingVariable));
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
            Assert.AreEqual(2, attExceptions.Count);

            // Inline file missing
            Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
            Console.WriteLine("Missing inline attachment files: " +
                              string.Join(", ", (attExceptions[0] as MailMergeMessage.AttachmentException)?.BadAttachment!));
            Console.WriteLine();
            Assert.AreEqual(1, (attExceptions[0] as MailMergeMessage.AttachmentException)?.BadAttachment.Count);

            // 2 file attachments missing
            Console.WriteLine($"{nameof(MailMergeMessage.AttachmentException)} thrown successfully:");
            Console.WriteLine("Missing attachment files: " +
                              string.Join(", ", (attExceptions[1] as MailMergeMessage.AttachmentException)?.BadAttachment!));
            Console.WriteLine();
            Assert.AreEqual(2, (attExceptions[1] as MailMergeMessage.AttachmentException)?.BadAttachment.Count);
        }

        // **************** Part 2:
        mmm.Config.IgnoreMissingInlineAttachments = true;
        mmm.Config.IgnoreMissingFileAttachments = true;
        // ************************

        try
        {
            mmm.GetMimeMessage(default);
            Assert.Fail("Expected exceptions not thrown.");
        }
        catch (MailMergeMessage.MailMergeMessageException exceptions)
        {
            /* Expected exceptions:
             * 1) 9 missing variables for {placeholders} and {:templates(...)}
             * 2) No recipients
             * 3) No FROM address
             * 4) 1 parsing error
             */
            Assert.AreEqual(4, exceptions.InnerExceptions.Count);
            Assert.IsFalse(exceptions.InnerExceptions.Any(e => e is MailMergeMessage.AttachmentException));

            Console.WriteLine("Exceptions for missing attachment files suppressed.");
        }
    }

    [Test]
    public void MessagesFromDataRows()
    {
        using var tbl = new DataTable();
        tbl.Columns.Add("Email", typeof(string));
        tbl.Columns.Add("Continent", typeof(string));
        tbl.Rows.Add("test@example.com", "Europe");
        tbl.Rows.Add("2ndRow@axample.com", "Asia");
        tbl.Rows.Add("3ndRow@axample.com", "America");
        var text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";

        using var mmm = new MailMergeMessage("Subject for {Continent}", text);
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Email}"));

        var i = 0;
        foreach (var mimeMessage in mmm.GetMimeMessages<DataRow>(tbl.Rows.OfType<DataRow>()))
        {
            DataRow row = tbl.Rows[i];
            Assert.IsTrue(mimeMessage.To.ToString().Contains(row["Email"].ToString()!));
            Assert.IsTrue(mimeMessage.TextBody.Contains(text
                .Replace("{Email}", row["Email"].ToString())
                .Replace("{Continent}", row["Continent"].ToString())));
            MailMergeMessage.DisposeFileStreams(mimeMessage);
            i++;
        }
    }

    [Test]
    public void MessagesFromListOfValueTuples()
    {
        var dataList = new List<ValueTuple<Dictionary<string, string>, Dictionary<string, string>>>();
        var t1 = (new Dictionary<string, string> { { "Email", "test@example.com" } }, new Dictionary<string, string> { { "Continent", "Europe" } });
        var t2 = (new Dictionary<string, string> { { "Email", "2ndRow@example.com" } }, new Dictionary<string, string> { { "Continent", "Asia" } });
        var t3 = (new Dictionary<string, string> { { "Email", "3ndRow@example.com" } }, new Dictionary<string, string> { { "Continent", "America" } });

        dataList.AddRange(new[] { t1, t2, t3 });

        const string text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";

        using var mmm = new MailMergeMessage("Subject for {Continent}", text);
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Email}"));

        var i = 0;
        foreach (var mimeMessage in mmm.GetMimeMessages<ValueTuple<Dictionary<string, string>, Dictionary<string, string>>>(dataList))
        {
            var (emailPart, continentPart) = dataList[i];
            Assert.IsTrue(mimeMessage.To.ToString().Contains(((Dictionary<string, string>)emailPart)["Email"]));
            Assert.IsTrue(mimeMessage.TextBody.Contains(text.Replace("{Email}", emailPart["Email"]).Replace("{Continent}", continentPart["Continent"])));
            MailMergeMessage.DisposeFileStreams(mimeMessage);
            i++;
        }
    }

    [Test]
    public void SingleMessageFromValueTuple()
    {
        var anonymous = new { Email = "test@example.com" };
        const string text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
        var so = (anonymous, new Dictionary<string, string> { { "Continent", "Europe" } });

        using var mmm = new MailMergeMessage("Subject for {Continent}", text);
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Email}"));

        var mimeMessage = mmm.GetMimeMessage(so);
        var (_, continentPart) = so;

        Assert.IsTrue(mimeMessage.To.ToString().Contains(anonymous.Email));
        Assert.IsTrue(mimeMessage.TextBody.Contains(text.Replace("{Email}", anonymous.Email).Replace("{Continent}", continentPart["Continent"])));
        MailMergeMessage.DisposeFileStreams(mimeMessage);
    }

    private class Recipient
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

    }

    [Test]
    public void MessagesFromList()
    {
        var recipients = new List<Recipient>();
        for (var i = 0; i < 10; i++)
        {
            recipients.Add(new Recipient() { Email = $"recipient-{i}@example.com", Name = $"Name of {i}" });
        }

        using var mmm = new MailMergeMessage("Get MimeMessages Test", string.Empty,
            "<html><head></head><body>This is the plain text part for {Name} ({Email})</body></html>");
        mmm.ConvertHtmlToPlainText();
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

        var cnt = 0;
        foreach (var mimeMessage in mmm.GetMimeMessages<Recipient>(recipients))
        {
            Assert.IsTrue(mimeMessage.TextBody ==
                          string.Format(
                              $"This is the plain text part for {recipients[cnt].Name} ({recipients[cnt].Email})"));
            Assert.IsTrue(mimeMessage.HtmlBody.Contains(string.Format(
                $"This is the plain text part for {recipients[cnt].Name} ({recipients[cnt].Email})")));
            Assert.IsTrue(mimeMessage.To.ToString().Contains(recipients[cnt].Name) &&
                          mimeMessage.To.ToString().Contains(recipients[cnt].Email));
            MailMergeMessage.DisposeFileStreams(mimeMessage);
            cnt++;

            // The message could be sent using the low-level API using a configured SmtpClient:
            // new SmtpClient().Send(FormatOptions.Default, mimeMessage);
        }
    }

    [Test]
    public void MessagesFromJsonArray()
    {
        var recipients = JArray.Parse(@"
[
    {
      'Email': 'email.1@example.com',
      'Name': 'John'
    },
    {
      'Email': 'email.2@example.com',
      'Name': 'Mary'
    },
    {
      'Email': 'email.3@example.com',
      'Name': 'Steve'
    }
]
");
        using var mmm = new MailMergeMessage("Get MimeMessages JSON Test", string.Empty, "<html><head></head><body>This is the plain text part for {Name} ({Email})</body></html>");
        mmm.ConvertHtmlToPlainText();
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}", "{Email}"));

        var cnt = 0;
        foreach (var mimeMessage in mmm.GetMimeMessages(recipients))
        {
            Assert.IsTrue(mimeMessage.TextBody == string.Format($"This is the plain text part for {recipients[cnt]["Name"]} ({recipients[cnt]["Email"]})"));
            Assert.IsTrue(mimeMessage.HtmlBody.Contains(string.Format($"This is the plain text part for {recipients[cnt]["Name"]} ({recipients[cnt]["Email"]})")));
            Assert.IsTrue(mimeMessage.To.ToString().Contains(recipients[cnt]["Name"]!.ToString()) && mimeMessage.To.ToString().Contains(recipients[cnt]["Email"]!.ToString()));
            MailMergeMessage.DisposeFileStreams(mimeMessage);
            cnt++;
        }
    }

    [Test]
    public void Disabled_Formatter_Should_Maintain_Variable_Placeholders()
    {
        var anonymous = new { Email = "test@example.com" };
        const string text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
        var so = (anonymous, new Dictionary<string, string> { { "Continent", "Europe" } });

        using var mmm = new MailMergeMessage("Subject for {Continent}", text)
        {
            EnableFormatter = false
        };
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "from@example.com"));
        mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "{Name}","to@example.com"));

        var mimeMessage = mmm.GetMimeMessage(so);
        var (_, continentPart) = so;

        Assert.IsTrue(mimeMessage.Subject.Contains("Subject for {Continent}"));
        Assert.IsTrue(mimeMessage.To.ToString().Contains("{Name}"));
        Assert.IsTrue(mimeMessage.TextBody.Contains(text));
        MailMergeMessage.DisposeFileStreams(mimeMessage);
    }
}