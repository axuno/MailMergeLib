using System;
using System.IO;
using MailMergeLib;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Message_Config
    {
        private MessageConfig _msgConfig = new MessageConfig();
        private string _tempPath = Path.GetTempPath();

        [TestCase(" \t", "?")]
        [TestCase(" ", "?")]
        [TestCase("", "?")]
        [TestCase(null, "?")]
        [TestCase("\\noFullPath", null)]
        [TestCase("C:\\some\\path\\to\\folder", "C:\\some\\path\\to\\folder")]
        public void SetFileBaseDirectory(string path, string expected)
        {
            if (expected == "?") expected = Path.GetTempPath();
            if (expected == null)
            {
                Assert.Throws<ArgumentException>(() => _msgConfig.FileBaseDirectory = path);
                return;
            }

            _msgConfig.FileBaseDirectory = path;
            Assert.AreEqual(expected, _msgConfig.FileBaseDirectory);
        }
    }
}
