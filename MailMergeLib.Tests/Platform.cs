using System;
using System.IO;
using NUnit.Framework;

namespace MailMergeLib.Tests
{
    [TestFixture]
    public class Platform
    {
        private const string _doesNotExist = "nothing-that-should-really-exist";

        [Test]
        public void Indentify_Windows_Platform()
        {
            var currentValues = (MailMergeLib.Platform.WinEnvironmentVariable,
                MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile);

            MailMergeLib.Platform.LinuxIdentifyingFile = _doesNotExist;
            MailMergeLib.Platform.MacOsxIdentifyingFile = _doesNotExist;

            MailMergeLib.Platform.WinEnvironmentVariable = "makes-only-sense-for-this-test";
            Environment.SetEnvironmentVariable(MailMergeLib.Platform.WinEnvironmentVariable, Path.GetDirectoryName(Path.GetTempFileName()));

            MailMergeLib.Platform.DeterminePlatform();
            Assert.IsTrue(MailMergeLib.Platform.OpSys == OpSys.Win);

            (MailMergeLib.Platform.WinEnvironmentVariable,
                    MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile) =
                currentValues;
        }

        [Test]
        public void Indentify_Linux_Platform()
        {
            var currentValues = (MailMergeLib.Platform.WinEnvironmentVariable,
                MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile);

            MailMergeLib.Platform.WinEnvironmentVariable = _doesNotExist;
            MailMergeLib.Platform.MacOsxIdentifyingFile = _doesNotExist;
            MailMergeLib.Platform.LinuxIdentifyingFile = Path.GetTempFileName();
            
            File.WriteAllText(MailMergeLib.Platform.LinuxIdentifyingFile, "Linux");

            MailMergeLib.Platform.DeterminePlatform();
            Assert.IsTrue(MailMergeLib.Platform.OpSys == OpSys.Linux);

            (MailMergeLib.Platform.WinEnvironmentVariable,
                    MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile) =
                currentValues;
        }

        [Test]
        public void Indentify_MacOsX_Platform()
        {
            var currentValues = (MailMergeLib.Platform.WinEnvironmentVariable,
                MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile);

            MailMergeLib.Platform.WinEnvironmentVariable = _doesNotExist;
            MailMergeLib.Platform.LinuxIdentifyingFile = _doesNotExist;
            MailMergeLib.Platform.MacOsxIdentifyingFile = Path.GetTempFileName();
            
            MailMergeLib.Platform.MacOsxIdentifyingFile = Path.GetTempFileName();

            MailMergeLib.Platform.DeterminePlatform();
            Assert.IsTrue(MailMergeLib.Platform.OpSys == OpSys.MacOsX);

            (MailMergeLib.Platform.WinEnvironmentVariable,
                    MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile) =
                currentValues;
        }

        [Test]
        public void Indentify_No_Platform()
        {
            var currentValues = (MailMergeLib.Platform.WinEnvironmentVariable,
                MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile);

            MailMergeLib.Platform.WinEnvironmentVariable = _doesNotExist;
            MailMergeLib.Platform.LinuxIdentifyingFile = _doesNotExist;
            MailMergeLib.Platform.MacOsxIdentifyingFile = _doesNotExist;

            Assert.Throws<UnsupportedPlatformException>(MailMergeLib.Platform.DeterminePlatform);

            (MailMergeLib.Platform.WinEnvironmentVariable,
                    MailMergeLib.Platform.LinuxIdentifyingFile, MailMergeLib.Platform.MacOsxIdentifyingFile) =
                currentValues;
        }

    }
}
