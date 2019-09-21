using System;
using System.IO;
using System.Reflection;

namespace UnitTests
{
    internal class Helper
    {
        /// <summary>
        /// Gets the path of code base for the executing assembly.
        /// </summary>
        /// <remarks>
        /// The Assembly.Location property sometimes gives wrong results when using NUnit (where assemblies run from a temporary folder).
        /// That's why we need reliable way to find the assembly location, which is the base for relativ data folders.
        /// </remarks>
        /// <returns></returns>
        public static string GetCodeBaseDirectory()
        {
            return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }

        internal static int Compare(Stream a, Stream b)
        {
            if (a == null && b == null) return 0;

            if (a == null || b == null) throw new ArgumentNullException(a == null ? "a" : "b");

            a.Position = b.Position = 0;

            if (a.Length < b.Length) return -1;

            if (a.Length > b.Length) return 1;

            int bufa;
            while ((bufa = a.ReadByte()) != -1)
            {
                var bufb = b.ReadByte();
                var diff = bufa.CompareTo(bufb);
                if (diff != 0) return diff;
            }
            return 0;
        }
    }
}
