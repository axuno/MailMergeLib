namespace MailMergeLib
{
    /// <summary>
    /// Class for information about file attachments for mail messages created from strings.
    /// </summary>
    public class StringAttachment
    {
        /// <summary>
        /// Creates a new file attachment information
        /// </summary>
        /// <param name="content">Content for the attachment</param>
        /// <param name="displayName">Name and extension as the reader of the mail should see it (in case of inline attachments displayName is used for CIDs</param>
        /// <param name="mimeType">Mime type of the file</param>
        public StringAttachment(string content, string displayName, string mimeType)
        {
            Content = content;
            DisplayName = displayName;
            MimeType = string.IsNullOrEmpty(mimeType) ? MimeKit.MimeTypes.GetMimeType(displayName) : mimeType;
        }

        /// <summary>
        /// Creates a new file attachment information
        /// </summary>
        /// <param name="content">Content for the attachment</param>
        /// <param name="displayName">Name and extension as the reader of the mail should see it (in case of inline attachments displayName is used for CIDs</param>
        public StringAttachment(string content, string displayName)
        {
            Content = content;
            DisplayName = displayName;
            MimeType = MimeKit.MimeTypes.GetMimeType(displayName);
        }

        /// <summary>
        /// Gets the content of the attachment
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// Gets the name used in the attachment (used for Cid with inline attachments)
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets the MimeType as a string, like "text/plain"
        /// </summary>
        public string MimeType { get; private set; }
    }
}