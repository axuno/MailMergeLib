using System.IO;

namespace MailMergeLib.Tests
{
    internal static class TestFileFolders
    {
        public const string PathRelativeToCodebase = @"..\..\..\TestFiles\";

        public static string FilesAbsPath = Path.GetFullPath(Path.Combine(Helper.GetCodeBaseDirectory(), PathRelativeToCodebase));
    }
}
