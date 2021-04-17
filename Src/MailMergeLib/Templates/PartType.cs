namespace MailMergeLib.Templates
{
    /// <summary>
    /// The type of a <see cref="Part"/>.
    /// </summary>
    /// <code>See the code sample for <see cref="Templates"/></code>
    public enum PartType
    {
        /// <summary>
        /// Plain text, which can be used in a plain text or in a HTML context.
        /// </summary>
        Plain,
        /// <summary>
        /// HTML text, which can be used in a HTML context only.
        /// </summary>
        Html
    }
}