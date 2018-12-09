using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailMergeLib;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Crypto
    {
        [Test]
        public void IvGetSet()
        {
            var oldValue = MailMergeLib.Crypto.IV;
            var test = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
            MailMergeLib.Crypto.IV = test;

            Assert.AreEqual(test, MailMergeLib.Crypto.IV);
            MailMergeLib.Crypto.IV = oldValue;
        }

        [Test]
        public void KeyGetSet()
        {
            var oldValue = MailMergeLib.Crypto.CryptoKey;
            var key = "some-random-key-for-testing";
            MailMergeLib.Crypto.CryptoKey = key;

            Assert.AreEqual(key, MailMergeLib.Crypto.CryptoKey);
            MailMergeLib.Crypto.CryptoKey = oldValue;
        }

        [Test]
        public void Encoding()
        {
            var oldValue = MailMergeLib.Crypto.Encoding;
            var encoding = System.Text.Encoding.BigEndianUnicode;
            MailMergeLib.Crypto.Encoding = encoding;

            Assert.AreEqual(encoding, MailMergeLib.Crypto.Encoding);
            MailMergeLib.Crypto.Encoding = oldValue;
        }


        [Test]
        public void EncryptDecrypt()
        {
            const string someValue = "some-random-value-for-testing";
            var encrypted = MailMergeLib.Crypto.Encrypt(someValue);

            Assert.AreEqual(someValue, MailMergeLib.Crypto.Decrypt(encrypted));
        }
    }
}