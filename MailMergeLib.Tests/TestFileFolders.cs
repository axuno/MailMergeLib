using System.IO;

namespace MailMergeLib.Tests
{
    internal static class TestFileFolders
    {
        public static string PathRelativeToCodebase {
            get{
                char slash = Path.DirectorySeparatorChar;
                return $"..{slash}..{slash}..{slash}TestFiles{slash}";
            }
        }

        public static string FilesAbsPath = Path.GetFullPath(Path.Combine(Helper.GetCodeBaseDirectory(), PathRelativeToCodebase));
    }
}
