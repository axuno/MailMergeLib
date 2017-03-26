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
    }
}
