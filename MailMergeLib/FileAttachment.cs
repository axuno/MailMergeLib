namespace MailMergeLib
{
    /// <summary>
    /// Class for file attachments of mail messages
    /// </summary>
    public class FileAttachment
    {
        /// <summary>
        /// Creates a new file attachment information
        /// </summary>
        /// <param name="fileName">Full path of the file</param>
        /// <param name="displayName">Name and extension as the reader of the mail should see it (in case of inline attachments displayName is used for CIDs</param>
        /// <param name="mimeType">Mime type of the file</param>
        public FileAttachment(string fileName, string displayName, string mimeType)
        {
            Filename = fileName;
            DisplayName = displayName;
            MimeType = string.IsNullOrEmpty(mimeType) ? MimeKit.MimeTypes.GetMimeType(fileName) : mimeType;
        }

        /// <summary>
        /// Creates a new file attachment information
        /// </summary>
        /// <param name="fileName">Full path of the file </param>
        /// <param name="displayNameOrCid">Name and extension as the reader of the mail should see it (in case of inline attachments displayName is used for CIDs</param>
        public FileAttachment(string fileName, string displayNameOrCid)
        {
            Filename = fileName;
            DisplayName = displayNameOrCid;
            MimeType = MimeKit.MimeTypes.GetMimeType(fileName);
        }

        /// <summary>
        /// Gets the name of the file in the file system
        /// </summary>
        public string Filename
        {
            get; private set;
        }

        /// <summary>
        /// Gets the name used in the attachment (used for Cid with inline attachments)
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets the MimeType as a string, like "text/plain"
        /// </summary>
        public string MimeType { get; private set; }

        /// <summary>
        /// Determines whether the specified FileAttachment instances are equal.
        /// </summary>
        /// <remarks>E.g. necessary for HashSet&lt;FileAttachment&gt;.</remarks>
        /// <param name="obj"></param>
        /// <returns>Returns true, if both FileAttachments are equal, else false.</returns>
        public override bool Equals(object obj)
        {
            var otherFa = obj as FileAttachment;
            if (otherFa == null) return false;

            return otherFa.Filename == Filename && otherFa.DisplayName == DisplayName && otherFa.MimeType == MimeType;
        }

        /// <summary>
        /// The HashCode for the FileAttachment.
        /// </summary>
        /// <returns>Returns the HashCode for the FileAttachment.</returns>
        /// <remarks>E.g. necessary for HashSet&lt;FileAttachment&gt;.</remarks>
        public override int GetHashCode()
        {
            return string.Concat(Filename, DisplayName, MimeType).GetHashCode();
        }
    }
}