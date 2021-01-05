using System.IO;
using System.Runtime;

namespace MailMergeLib.Tests
{
    internal static class TestFileFolders
    {
        private static string Get_TestFiles_Path_From_CodeBase()
        {
            var codeBaseDir = Helper.GetCodeBaseDirectory();
            var di = new DirectoryInfo(codeBaseDir);
            while (di.Parent != null)
            {
                di = di.Parent;
                var testFileDirectory = Path.Combine(di.FullName, "TestFiles" + Path.DirectorySeparatorChar);
                if (Directory.Exists(testFileDirectory)) return testFileDirectory;
            }

            return di.Root.FullName;
        }

        public static string FilesAbsPath = Get_TestFiles_Path_From_CodeBase();
    }
}
