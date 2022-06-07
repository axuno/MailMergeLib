using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Tools
{
    [Test]
    [TestCase(@"C:\dir\file.ext", true, ExcludePlatform="Linux")]
    [TestCase(@"C:\dir\", true, ExcludePlatform="Linux")]
    [TestCase(@"C:\dir", true, ExcludePlatform="Linux")]
    [TestCase(@"C:\", true, ExcludePlatform="Linux")]
    [TestCase(@"\\unc\share\dir\file.ext", true, ExcludePlatform="Linux")]
    [TestCase(@"\\unc\share", true, ExcludePlatform="Linux")]
    [TestCase(@"C:", false, ExcludePlatform="Linux")]
    [TestCase(@"C:dir\file.ext", false, ExcludePlatform="Linux")]
    [TestCase(@"\dir\file.ext", false, ExcludePlatform="Linux")]
    [TestCase(@"\dir", false, ExcludePlatform="Linux")] // An "absolute", but not "full" path
    [TestCase(@"file.ext", false)]
    [TestCase(@"dir\file.ext", false)]
    [TestCase(null, false, false)]
    [TestCase("", false, false)]
    [TestCase("   ", false, false, ExcludePlatform="Linux")]


    [TestCase(@"/dir", true, IncludePlatform="Linux")] // An "absolute", "full" path
    [TestCase(@"/dir/file.ext", true, IncludePlatform="Linux")]

#if !NETCOREAPP
        // does not throw for net core
        [TestCase(@"C:\inval|d", false, false, ExcludePlatform="Linux")]
        [TestCase(@"\\is_this_a_dir_or_a_hostname", false, false, ExcludePlatform="Linux")]
#endif
    public static void IsFullPath(string path, bool expectedIsFull, bool expectedIsValid = true)
    {
        Assert.AreEqual(expectedIsFull, MailMergeLib.Tools.IsFullPath(path), "IsFullPath('" + path + "')");

        if (expectedIsFull)
        {
            Assert.AreEqual(path, Path.GetFullPath(path));
        }
        else if (expectedIsValid)
        {
            Assert.AreNotEqual(path, Path.GetFullPath(path));
        }
        else
        {
            Assert.That(() => Path.GetFullPath(path), Throws.Exception);
        }
    }

    [Test]
    [TestCase(@"..\..", @"folder1\folder2\folder3\", @"folder1", ExcludePlatform="Linux")]
    [TestCase(@"", @"folder1\folder2\folder3\", @"folder1\folder2\folder3\", ExcludePlatform="Linux")]
    [TestCase(@"folder2\folder3", @"folder1\", @"folder1\folder2\folder3", ExcludePlatform="Linux")]
    [TestCase(@"../..", @"folder1/folder2/folder3/", @"folder1", IncludePlatform="Linux")]
    [TestCase(@"", @"folder1/folder2/folder3/", @"folder1/folder2/folder3/", IncludePlatform="Linux")]
    [TestCase(@"folder2/folder3", @"folder1/", @"folder1/folder2/folder3", IncludePlatform="Linux")]
    public void RelativePathTo(string expected, string from, string to)
    {
        Assert.AreEqual(expected, MailMergeLib.Tools.RelativePathTo(from, to));
    }

    [Test]
    [TestCase(null, @"C:\Temp", @"D:\", ExcludePlatform="Linux")]
    public void RelativePathTo_Different_Path_Roots(string expected, string from, string to)
    {
        Assert.Throws<ArgumentException>(() => { MailMergeLib.Tools.RelativePathTo(from, to); });
    }

    [Test]
    [TestCase(null, @"folder1\", @"folder2", ExcludePlatform="Linux")]
    [TestCase(null, @"folder1/", @"folder2", IncludePlatform="Linux")]
    public void RelativePathTo_No_Common_Prefix_Path(string expected, string from, string to)
    {
        Assert.Throws<ArgumentException>(() => { MailMergeLib.Tools.RelativePathTo(from, to); });
    }

    [Test]
    [TestCase(null, null, "")]
    [TestCase(null, "", null)]
    public void RelativePathTo_NullTests(string expected, string from, string to)
    {
        Assert.Throws<ArgumentNullException>(() => { MailMergeLib.Tools.RelativePathTo(from, to); });
    }


    [Test]
    [TestCase(true, "abcdef")]
    [TestCase(false, "abcdeföäü")]
    public void IsSevenBit(bool expected, string toTest)
    {
        Assert.AreEqual(expected, MailMergeLib.Tools.IsSevenBit(toTest));
    }

    [Test]
    [TestCase(true, "abcdef")]
    [TestCase(false, "abcdeföäü")]
    public void IsSevenBitStream(bool expected, string toTest)
    {
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(toTest ?? string.Empty)))
        {
            Assert.AreEqual(expected, MailMergeLib.Tools.IsSevenBit(stream));
        }
    }

    [Test]
    [TestCase("Some Text")]
    public void StreamToString(string toTest)
    {
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(toTest ?? string.Empty)))
        {
            Assert.AreEqual(toTest, MailMergeLib.Tools.Stream2String(stream));
        }
    }

    [Test]
    public void WrapLines()
    {
        var text = "this is a number of words in one line which will be wrapped 1234567890 1234";
        Assert.AreEqual(9, MailMergeLib.Tools.WrapLines(text, 10).Split(new []{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Test]
    [TestCase("test@example.com", "test@example.com", "")]
    [TestCase("Firstname Lastname <test@example.com>", "test@example.com", "Firstname Lastname")]
    public void ParseMailAddress(string toTest, string expectedEmail, string expectedDisplayName)
    {
        MailMergeLib.Tools.ParseMailAddress(toTest, out var displayName, out var email);
        Assert.AreEqual(expectedEmail, email);
        Assert.AreEqual(expectedDisplayName, displayName);
    }

    [Test]
    [TestCase("utf-8", "utf-8")]
    [TestCase("utf-32", "utf-32")]
    [TestCase("utf-16", "utf-16")]
    [TestCase("iso-8859-1", "iso-8859-1")]
#if !NETCOREAPP
        [TestCase("windows-1252", "windows-1252")]
        [TestCase("iso-2022-jp", "shift_jis")]
        [TestCase("iso-2022-jp", "csISO2022JP")]
        [TestCase("euc-kr", "ks_c_5601-1987")]
        [TestCase("euc-kr", "iso-2022-kr")]
#endif
    public void GetMimeCharset(string expected, string encoding)
    {
        Assert.AreEqual(expected, MailMergeLib.Tools.GetMimeCharset(Encoding.GetEncoding(encoding)));
        Assert.Throws<ArgumentNullException>(() => MailMergeLib.Tools.GetMimeCharset(null));
    }
}