using System;
using System.IO;

namespace MailMergeLib{
    public static class Platform{
        static Platform(){
            string windir = Environment.GetEnvironmentVariable("windir");
            if (!string.IsNullOrEmpty(windir) && windir.Contains(@"\") && Directory.Exists(windir))
            {
                IsWindows = true;
            }
            else if (File.Exists(@"/proc/sys/kernel/ostype"))
            {
                string osType = File.ReadAllText(@"/proc/sys/kernel/ostype");
                if (osType.StartsWith("Linux", StringComparison.OrdinalIgnoreCase))
                {
                    // Note: Android gets here too
                    IsLinux = true;
                }
                else
                {
                    throw new UnsupportedPlatformException(osType);
                }
            }
            else if (File.Exists(@"/System/Library/CoreServices/SystemVersion.plist"))
            {
                // Note: iOS gets here too
                IsMacOsX = true;
            }
            else
            {
                throw new UnsupportedPlatformException("Unknown");
            }
        }

        public static bool IsWindows{get;}

        public static bool IsLinux{get;}

        public static bool IsMacOsX{get;}
    }
}