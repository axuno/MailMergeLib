namespace MailMergeLib
{
    /// <summary>
    /// Convert HTML to plain text.
    /// </summary>
    public interface IHtmlConverter
    {
        /// <summary>
        /// Convert a text file with HTML content to plain text.
        /// </summary>
        /// <param name="html">The HTML string to convert.</param>
        /// <returns>The plain text representation of the HTML content.</returns>
        string ToPlainText(string html);
    }
}