using System;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class EmailValidator
    {
        [Test]
        [TestCase(true, "test@example.com", false, false)]
        [TestCase(true, "test@subdomain.example.com", false, false)]
        [TestCase(false, "test@.example.com", false, false)]
        [TestCase(false, "\"test@.example.com\"", false, false)]
        [TestCase(true, "test@192.168.100.1", false, false)]
        [TestCase(false, "test@2001:0db8:1234:ffff:ffff:ffff:ffff:ffff", false, false)]
        [TestCase(true, "test@[ipv6:2001:0db8:1234:ffff:ffff:ffff:ffff:ffff]", false, false)]
        [TestCase(true, "test@com", true, false)]
        [TestCase(true, "öäü@com", true, true)]
        [TestCase(false, "test@com", false, false)]
        [TestCase(false, "öäü@com", true, false)]
        [TestCase(false, "@exemple.com", false, false)]
        [TestCase(true, "a@b", true, false)]
        [TestCase(false, "a@b", false, false)]
        [TestCase(false, ".", false, false)]
        [TestCase(false, "123456789-123456789-123456789-123456789-123456789-123456789-123456789@example.com", false, false)]
        public void Run(bool correct, string email, bool allowTopLevelDomains, bool allowInternational)
        {
            if (email == null) Assert.Throws<ArgumentNullException>(() => { MailMergeLib.EmailValidator.Validate(email, allowTopLevelDomains, allowInternational); });
            Assert.AreEqual(correct, MailMergeLib.EmailValidator.Validate(email, allowTopLevelDomains, allowInternational));
        }
    }
}
