using System.IO;
using MailMergeLib.SmartFormatMail.Core.Extensions;

namespace MailMergeLib.SmartFormatMail.Core.Output
{
    /// <summary>
    /// Wraps a TextWriter so that it can be used for output.
    /// </summary>
    [System.Obsolete("Use classes in namespace 'SmartFormat' instead of 'MailMergeLib.SmartFormatMail'", false)] public class TextWriterOutput : IOutput
    {
        public TextWriterOutput(TextWriter output)
        {
            Output = output;
        }

        public TextWriter Output { get; }

        public void Write(string text, IFormattingInfo formattingInfo)
        {
            Output.Write(text);
        }

        public void Write(string text, int startIndex, int length, IFormattingInfo formattingInfo)
        {
            Output.Write(text.Substring(startIndex, length));
        }
    }
}