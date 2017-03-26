using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MailMergeLib
{
    internal class RegExHtmlConverter : IHtmlConverter
    {
        #region IHtmlConverter Members

        /// <summary>
        /// Converts HTML to text as good as a simple RegEx based function can.
        /// Html entities are not (!) converted to text.
        /// Not very useful for complex HTML, because regular expressions
        /// are not the right means to deal with HTML. Use parsers like 
        /// AngleSharp or HtmlAgilityPack instead of this "poor man's HTML converter".
        /// </summary>
        /// <param name="html">Html text</param>
        /// <returns>Text without html tags</returns>
        public string ToPlainText(string html)
        {
            var sb = new StringBuilder(html);

            // remove characters ignored by web browsers
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '\n' || sb[i] == '\r' || sb[i] == '\t')
                    sb.Remove(i--, 1);
            }

            // convert non-breaking spaces to space
            sb = sb.Replace("&nbsp;", " ");

            // replace repeating spaces, also ignored by browsers
            string result = sb.Replace("  ", " ").ToString();

            // Remove the header
            result = Regex.Replace(result, @"<\s*head[^>]*>.*</\s*head\s*>", string.Empty,
                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Remove the scripts
            // result = Regex.Replace(result, @"<\s*script[^>]*>.*</\s*script\s*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // remove styles
            result = Regex.Replace(result, @"<\s*style[^>]*>.*</\s*style\s*>", string.Empty,
                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // replace "br" and "li" tags with line breaks
            result = Regex.Replace(result, @"<\s*br[^>]*>", Environment.NewLine, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*li[^>]*>", Environment.NewLine, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // remove "html", "body", "form" and "span" tags
            result = Regex.Replace(result, @"<\s*html[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*body[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*form[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*span[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // replace "p", "div" and "tr" tags with double line breaks
            result = Regex.Replace(result, @"<\s*p[^>]*>", Environment.NewLine + Environment.NewLine,
                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*div[^>]*>", Environment.NewLine + Environment.NewLine,
                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);
            result = Regex.Replace(result, @"<\s*tr[^>]*>", Environment.NewLine + Environment.NewLine,
                                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // replace "td" tags with tabs
            result = Regex.Replace(result, @"<\s*td[^>]*>", "\t", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // replace all other html tags
            result = Regex.Replace(result, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // replace html entities with text: poor man cannot afford :)

            return new StringBuilder(result).ToString();
        }

        #endregion
    }
}