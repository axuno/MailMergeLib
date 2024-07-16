using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Crypto
{
    [Test]
    public void IvGetSet()
    {
        var oldValue = MailMergeLib.Crypto.IV;
        var test = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };
        MailMergeLib.Crypto.IV = test;

        Assert.That(MailMergeLib.Crypto.IV, Is.EqualTo(test));
        MailMergeLib.Crypto.IV = oldValue;
    }

    [Test]
    public void KeyGetSet()
    {
        var oldValue = MailMergeLib.Crypto.CryptoKey;
        var key = "some-random-key-for-testing";
        MailMergeLib.Crypto.CryptoKey = key;

        Assert.That(MailMergeLib.Crypto.CryptoKey, Is.EqualTo(key));
        MailMergeLib.Crypto.CryptoKey = oldValue;
    }

    [Test]
    public void Encoding()
    {
        var oldValue = MailMergeLib.Crypto.Encoding;
        var encoding = System.Text.Encoding.BigEndianUnicode;
        MailMergeLib.Crypto.Encoding = encoding;

        Assert.That(MailMergeLib.Crypto.Encoding, Is.EqualTo(encoding));
        MailMergeLib.Crypto.Encoding = oldValue;
    }


    [Test]
    public void EncryptDecrypt()
    {
        const string someValue = "some-random-value-for-testing";
        var encrypted = MailMergeLib.Crypto.Encrypt(someValue);

        Assert.That(MailMergeLib.Crypto.Decrypt(encrypted), Is.EqualTo(someValue));
    }
}
