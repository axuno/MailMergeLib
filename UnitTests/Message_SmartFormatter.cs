using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using MailMergeLib;
using MailMergeLib.SmartFormatMail.Core.Parsing;
using MailMergeLib.SmartFormatMail.Core.Settings;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Message_SmartFormatter
    {
        private class TestClass
        {
            public string Email { set; get; } = "test@example.com";
            public string GetContinent()
            {
                return "Europe";
            }
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

            var smf = mmm.SmartFormatter;
            Assert.AreEqual(dataItem.Email, smf.Format("{Email}", dataItem));
            Assert.AreNotEqual(dataItem.Email, smf.Format("{EmAiL}", dataItem));
            // The following is the same as smf.Settings.CaseSensitivity = CaseSensitivityType.CaseInsensitive
            mmm.Config.SmartFormatterConfig.CaseSensitivity = CaseSensitivityType.CaseInsensitive;
            Assert.AreEqual(dataItem.Email, smf.Format("{EmAiL}", dataItem));
        }


        [Test]
        public void DataTypeTests()
        {
            // ******** Initialize ********
            string result;
            string expected;
            object dataItem;
            var culture = CultureInfo.GetCultureInfo("en-US");
            var smf = new MailSmartFormatter
            {
                Settings = { FormatErrorAction = ErrorAction.Ignore, ParseErrorAction = ErrorAction.ThrowError }
            };

            // ******** Class instances ********
            dataItem = new TestClass();
            var text = "Lorem ipsum dolor. Email={Email}, Continent={GetContinent}, City={GetNewTestClass.City}.";
            result = smf.Format(culture, text, dataItem);
            expected = string.Format($"Lorem ipsum dolor. Email={((TestClass)dataItem).Email}, Continent={((TestClass)dataItem).GetContinent()}, City={((TestClass)dataItem).GetNewTestClass().City}.");
            Assert.AreEqual(expected, result);
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
            Assert.AreEqual(expected, result);
            Console.WriteLine("Anonymous type: passed");

            // ******** List ********
            dataItem = new List<string>() { "Lorem", "ipsum", "dolor" };  // this works
            result = smf.Format("{0:list:{}|, |, and }", dataItem);
            expected = "Lorem, ipsum, and dolor";
            Assert.AreEqual(expected, result);
            Console.WriteLine("List: passed");

            // ******** Array ********
            dataItem = new[] { "Lorem", "ipsum", "dolor" };
            result = smf.Format("{0:list:{}|, |, and }", dataItem);
            expected = "Lorem, ipsum, and dolor";
            Assert.AreEqual(expected, result);
            Console.WriteLine("Array: passed");

            // ******** Dictionary ********
            dataItem = new Dictionary<string, object>() { { "Email", "test@example.com" }, {"Continent", "Europe"} };

            text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
            expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");
            result = smf.Format(culture, text, dataItem);
            Assert.AreEqual(expected,result);
            Console.WriteLine("Dictionary: passed");

            // ******** ExpandoObject ********
            dynamic em = new ExpandoObject();
            em.Email = "test@example.com";
            em.Continent = "Europe";
            dataItem = em;

            text = "Lorem ipsum dolor. Email={Email}, Continent={Continent}.";
            result = smf.Format(culture, text, dataItem);
            expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");
            Assert.AreEqual(expected, result);
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
                result = ex.MimeMessage.Subject;
            }
            
            expected = text.Replace("{Email}", "test@example.com").Replace("{Continent}", "Europe");

            Assert.AreEqual(expected, result);
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
                Assert.That(parsingErrors.Count == 1);
                Assert.That(ex.Message.Contains("In: \"{lorem\""));
                Console.WriteLine("Parsing error: passed");
            }

            // ******** Formatting error ********
            var missingVariables = new List<string>();
            smf.OnFormattingFailure += (sender, args) => { missingVariables.Add(args.Placeholder); };
            result = smf.Format(culture, "{lorem}", dataItem);
            
            Assert.That(missingVariables.Contains("{lorem}"));
            Console.WriteLine("Format error (missing variable): passed");

            // ******** Culture ********
            result = smf.Format(culture, "{Date:MMMM}", new DateTime(2016,01,01));
            
            Assert.AreEqual("January", result);
            Console.WriteLine("Culture: passed");
        }
    }
}
