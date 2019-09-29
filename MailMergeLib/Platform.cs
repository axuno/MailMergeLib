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
            var windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                OpSys = OpSys.Win;
            }
            else if (File.Exists("/proc/sys/kernel/ostype"))
            {
                var osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
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
            else if (File.Exists("/System/Library/CoreServices/SystemVersion.plist"))
            {
                // Note: iOS gets here, too
                OpSys = OpSys.MacOsX;
            }
            else
            {
                throw new UnsupportedPlatformException("Unknown");
            }
        }

        /// <summary>
        /// The operating system of the platform.
        /// </summary>
        public static OpSys OpSys { get; }
    }
}