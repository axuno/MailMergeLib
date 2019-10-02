using System;
using System.IO;

namespace MailMergeLib
{
    /// <summary>
    /// Supported operating system types of platforms.
    /// Member names must conform to https://github.com/nunit/docs/wiki/Platform-Attribute
    /// </summary>
    public enum OpSys
    {
        Win,
        Linux,
        MacOsX
    }

    /// <summary>
    /// This exception is thrown if the code runs on an unsupported platform.
    /// </summary>
    public class UnsupportedPlatformException : Exception
    {
        public UnsupportedPlatformException(string message) : base(message)
        { }
    }

    /// <summary>
    /// Determines the platform the code is running on (e.g. Win, Linux, MacOsX).
    /// </summary>
    public static class Platform
    {
        static Platform()
        {
            DeterminePlatform();
        }

        internal static void DeterminePlatform()
        {
            var windir = Environment.GetEnvironmentVariable(WinEnvironmentVariable);
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                OpSys = OpSys.Win;
            }
            else if (File.Exists(LinuxIdentifyingFile))
            {
                var osType = File.ReadAllText(LinuxIdentifyingFile);
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    // Note: Android gets here, too
                    OpSys = OpSys.Linux;
                }
                else
                {
                    throw new UnsupportedPlatformException(osType);
                }
            }
            else if (File.Exists(MacOsxIdentifyingFile))
            {
                // Note: iOS gets here, too
                OpSys = OpSys.MacOsX;
            }
            else
            {
                throw new UnsupportedPlatformException("Unknown");
            }
        }

        internal static string WinEnvironmentVariable { get; set; } = "windir";

        internal static string LinuxIdentifyingFile { get; set; } = "/proc/sys/kernel/ostype";

        internal static string MacOsxIdentifyingFile { get; set; } = "/System/Library/CoreServices/SystemVersion.plist";

        /// <summary>
        /// The operating system of the platform.
        /// </summary>
        public static OpSys OpSys { get; private set; }
    }
}