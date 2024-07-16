using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SmartFormat.Core.Parsing;
using SmartFormat.Core.Settings;

namespace MailMergeLib.Tests;

[TestFixture]
public class Message_SmartFormatter
{
    private class TestClass
    {
        public string Email { set; get; } = "test@example.com";
#pragma warning disable CA1822 
        public string GetContinent()
        {
            return "Europe";
        }
#pragma warning restore CA1822
        private TestClass2 NewTestClass { get; set; } = new TestClass2();
        public TestClass2 GetNewTestClass()
        {
            return NewTestClass;
        }
    }

    private class TestClass2
    {
        public string City { get; set; } = "New York";
    }

    [Test]
    public void PlaceholderCaseSensitivityTest()
    {
        var dataItem = new
        {
            Email = "test@example.com",
        };

        var mmm = new MailMergeMessage();
        mmm.Config.SmartFormatterConfig.CaseSensitivity = CaseSensitivityType.CaseSensitive;
        mmm.Config.SmartFormatterConfig.FormatErrorAction = ErrorAction.OutputErrorInResult;
        mmm.Config.SmartFormatterConfig.ParseErrorAction = ErrorAction.OutputErrorInResult;

        Assert.Multiple(() =>
        {
            Assert.That(mmm.SmartFormatter.Format("{Email}", dataItem), Is.EqualTo(dataItem.Email));
            Assert.That(mmm.SmartFormatter.Format("{EmAiL}", dataItem), Is.Not.EqualTo(dataItem.Email));
        });
        // Changing the the SmartFormatterConfig settings creates a new instance of
        // the SmartFormatter inside MailMergeMessage
        mmm.Config.SmartFormatterConfig.CaseSensitivity = CaseSensitivityType.CaseInsensitive;
        var actual = mmm.SmartFormatter.Format("{EmAiL}", dataItem);
        Assert.That(actual, Is.EqualTo(dataItem.Email));
    }


    [Test]
    public void DataTypeTests()
    {
        // ******** Initialize ********
        var culture = CultureInfo.GetCultureInfo("en-US");
        var smf = new MailSmartFormatter(new SmartFormatterConfig(), new SmartSettings());
        smf.Settings.Formatter.ErrorAction = FormatErrorAction.Ignore;
        smf.Settings.Parser.ErrorAction = ParseErrorAction.ThrowError;
        // ******** Class instances ********
        object dataItem = new TestClass();
        var text = "Lorem ipsum dolor. Email={Email}, Continent={GetContinent}, City={GetNewTestClass.City}.";
        var result = smf.Format(culture, text, dataItem);
        var expected = string.Format($"Lorem ipsum dolor. Email={((TestClass)dataItem).Email}, Continent={((TestClass)dataItem).GetContinent()}, City={((TestClass)dataItem).GetNewTestClass().City}.");
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("Class instances: passed");

        // ******** Anonymous type ********
        dataItem = new
        {
            Email = "test@example.com",
            Continent = "Europe",
            NewTestClass = new { City = "New York"}
        };

        text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}, City={NewTestClass.City}.";
        result = smf.Format(culture, text, dataItem);
        expected = "Lorem ipsum dolor. Email=test@example.com, Continent=Europe, City=New York.";
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("Anonymous type: passed");

        // ******** List ********
        dataItem = new List<string>() { "Lorem", "ipsum", "dolor" };  // this works
        result = smf.Format("{0:list:{}|, |, and }", dataItem);
        expected = "Lorem, ipsum, and dolor";
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("List: passed");

        // ******** Array ********
        dataItem = new[] { "Lorem", "ipsum", "dolor" };
        result = smf.Format("{0:list:{}|, |, and }", dataItem);
        expected = "Lorem, ipsum, and dolor";
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("Array: passed");

        // ******** Dictionary ********
        dataItem = new Dictionary<string, object>() { { "Email", "test@example.com" }, {"Continent", "Europe"} };

        text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
        expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");
        result = smf.Format(culture, text, dataItem);
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("Dictionary: passed");

        // ******** JSON ********
        // JObject
        dataItem = JObject.Parse("{ 'Email':'test@example.com', 'Continent':'Europe' }");
        expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");
        result = smf.Format(culture, text, dataItem);
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("JSON Object: passed");
        // JArray
        dataItem = JObject.Parse(@"
{
  'Manufacturers': [
    {
      'Name': 'Acme Corp'
    },
    {
      'Name': 'Contoso'
    },
    {
      'Name': 'Jumbo'
    }
  ]
}
");
        expected = "Acme Corp, Contoso, and Jumbo";
        result = smf.Format(culture, "{Manufacturers:list:{Name}|, |, and }", dataItem);
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("JSON Array: passed");

        // ******** ExpandoObject ********
        dynamic em = new ExpandoObject();
        em.Email = "test@example.com";
        em.Continent = "Europe";
        dataItem = em;

        text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
        result = smf.Format(culture, text, dataItem);
        expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");
        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("ExpandoObject: passed");

        // ******** DataRow ********
        var tbl = new DataTable();
        tbl.Columns.Add("Email", typeof(string));
        tbl.Columns.Add("Continent", typeof(string));
        tbl.Rows.Add("test@example.com", "Europe");
        dataItem = tbl.Rows[0];
        text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
        // this is part of MailMergeMessage.GetMimeMessage() because MailSmartFormatter does not support TableRows on its own
        // dataItem = row.Table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c]);
        try
        {
            new MailMergeMessage(text, string.Empty, string.Empty)
            {
                Config = {CultureInfo = culture, IgnoreIllegalRecipientAddresses = true}
            }.GetMimeMessage(dataItem); // will throw exception
        }
        catch (MailMergeMessage.MailMergeMessageException ex)
        {
            // will throw because of incomplete mail addresses, but Subject should contain placeholders replaced with content
            result = ex.MimeMessage?.Subject;
        }
            
        expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");

        Assert.That(result, Is.EqualTo(expected));
        Console.WriteLine("DataRow: passed");


        // ******** Parser error ********
        var parsingErrors = new List<string>();
        try
        {
            smf.Parser.OnParsingFailure += (sender, args) => { parsingErrors.Add(args.Errors.MessageShort); };
            result = smf.Format(culture, "{lorem", dataItem);
            Assert.Fail("No parsing error.");
        }
        catch (ParsingErrors ex)
        {
            Assert.Multiple(() =>
            {
                Assert.That(parsingErrors.Count == 1);
                Assert.That(ex.Message.Contains("In: \"{lorem\""));
            });
            Console.WriteLine("Parsing error: passed");
        }

        // ******** Formatting error ********
        var missingVariables = new List<string>();
        smf.OnFormattingFailure += (sender, args) => { missingVariables.Add(args.Placeholder); };
        result = smf.Format(culture, "{lorem}", dataItem);
            
        Assert.That(missingVariables.Contains("{lorem}"));
        Console.WriteLine("Format error (missing variable): passed");

        // ******** Culture ********
        result = smf.Format(culture, "{Date:d:MMMM}", new DateTime(2018,01,01));

        Assert.That(result, Is.EqualTo("January"));
        Console.WriteLine("Culture: passed");
    }
}
