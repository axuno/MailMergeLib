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
    public static void IsFullPath(string? path, bool expectedIsFull, bool expectedIsValid = true)
    {
        Assert.That(MailMergeLib.Tools.IsFullPath(path ?? string.Empty), Is.EqualTo(expectedIsFull), "IsFullPath('" + path + "')");

        if (expectedIsFull)
        {
            Assert.That(Path.GetFullPath(path ?? string.Empty), Is.EqualTo(path));
        }
        else if (expectedIsValid)
        {
            Assert.That(Path.GetFullPath(path ?? string.Empty), Is.Not.EqualTo(path ?? string.Empty));
        }
        else
        {
            Assert.That(() => Path.GetFullPath(path ?? string.Empty), Throws.Exception);
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
        Assert.That(MailMergeLib.Tools.RelativePathTo(from, to), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(null, @"C:\Temp", @"D:\", ExcludePlatform="Linux")]
    public void RelativePathTo_Different_Path_Roots(string? expected, string from, string to)
    {
        Assert.Throws<ArgumentException>(() => { MailMergeLib.Tools.RelativePathTo(from, to); });
    }

    [Test]
    [TestCase(null, @"folder1\", @"folder2", ExcludePlatform="Linux")]
    [TestCase(null, @"folder1/", @"folder2", IncludePlatform="Linux")]
    public void RelativePathTo_No_Common_Prefix_Path(string? expected, string from, string to)
    {
        Assert.Throws<ArgumentException>(() => { MailMergeLib.Tools.RelativePathTo(from, to); });
    }

    [Test]
    [TestCase(null, null, "")]
    [TestCase(null, "", null)]
    public void RelativePathTo_NullTests(string? expected, string? from, string? to)
    {
        Assert.Throws<ArgumentNullException>(() => { MailMergeLib.Tools.RelativePathTo(from!, to!); });
    }


    [Test]
    [TestCase(true, "abcdef")]
    [TestCase(false, "abcdeföäü")]
    public void IsSevenBit(bool expected, string toTest)
    {
        Assert.That(MailMergeLib.Tools.IsSevenBit(toTest), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(true, "abcdef")]
    [TestCase(false, "abcdeföäü")]
    public void IsSevenBitStream(bool expected, string toTest)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(toTest ?? string.Empty));
        Assert.That(MailMergeLib.Tools.IsSevenBit(stream), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("Some Text")]
    public void StreamToString(string toTest)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(toTest ?? string.Empty));
        Assert.That(MailMergeLib.Tools.Stream2String(stream), Is.EqualTo(toTest));
    }

    [Test]
    public void WrapLines()
    {
        var text = "this is a number of words in one line which will be wrapped 1234567890 1234";
        Assert.That(MailMergeLib.Tools.WrapLines(text, 10).Split(new []{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length, Is.EqualTo(9));
    }

    [Test]
    [TestCase("test@example.com", "test@example.com", "")]
    [TestCase("Firstname Lastname <test@example.com>", "test@example.com", "Firstname Lastname")]
    public void ParseMailAddress(string toTest, string expectedEmail, string expectedDisplayName)
    {
        MailMergeLib.Tools.ParseMailAddress(toTest, out var displayName, out var email);
        Assert.Multiple(() =>
        {
            Assert.That(email, Is.EqualTo(expectedEmail));
            Assert.That(displayName, Is.EqualTo(expectedDisplayName));
        });
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
        Assert.That(MailMergeLib.Tools.GetMimeCharset(Encoding.GetEncoding(encoding)), Is.EqualTo(expected));
        Assert.Throws<ArgumentNullException>(() => MailMergeLib.Tools.GetMimeCharset(null));
    }
}
