using System;

namespace MailMergeLib.SmartFormatMail.Utilities
{
    /// <summary>
    /// This class wraps a delegate, allowing it to be used as a parameter
    /// to any string-formatting method (such as <see cref="string.Format(string, object)" />).
    /// For example:
    /// <code>
    ///  var textWithLink = String.Format("Please click on {0:this link}.", new FormatDelegate((text) => Html.ActionLink(text, "SomeAction"));
    ///  </code>
    /// </summary>
    [System.Obsolete("Use classes in namespace 'SmartFormat' instead of 'MailMergeLib.SmartFormatMail'", false)] public class FormatDelegate : IFormattable
    {
        private readonly Func<string, string> getFormat1;
        private readonly Func<string, IFormatProvider, string> getFormat2;

        public FormatDelegate(Func<string, string> getFormat)
        {
            getFormat1 = getFormat;
        }

        public FormatDelegate(Func<string, IFormatProvider, string> getFormat)
        {
            getFormat2 = getFormat;
        }

        /// <summary>
        /// Implements System.IFormattable
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return getFormat1 != null ? getFormat1(format) : getFormat2(format, formatProvider);
        }
    }
}