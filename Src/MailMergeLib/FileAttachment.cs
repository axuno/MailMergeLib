namespace MailMergeLib;

/// <summary>
/// Class for file attachments of a <see cref="MailMergeMessage"/>
/// </summary>
public class FileAttachment
{
    /// <summary>
    /// Creates a new file attachment for a <see cref="MailMergeMessage"/>
    /// </summary>
    public FileAttachment()
    {
        DisplayName = Filename = string.Empty;
        MimeType = MimeKit.MimeTypes.GetMimeType( "application/octet-stream");
    }

    /// <summary>
    /// Creates a new file attachment for a <see cref="MailMergeMessage"/>
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
        get; set;
    }

    /// <summary>
    /// Gets the name used in the attachment (used for Cid with inline attachments)
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets the MimeType as a string, like "text/plain"
    /// </summary>
    public string MimeType { get; set; }

    #region *** Equality ***
        
    /// <summary>
    /// Determines whether the specified FileAttachment instances are equal.
    /// </summary>
    /// <remarks>E.g. necessary for HashSet&lt;FileAttachment&gt;.</remarks>
    /// <param name="fa"></param>
    /// <returns>Returns true, if both FileAttachments are equal, else false.</returns>
    public override bool Equals(object fa)
    {
        if (fa is null) return false;
        if (ReferenceEquals(this, fa)) return true;
        if (fa.GetType() != GetType()) return false;
        return Equals((FileAttachment) fa);
    }

    private bool Equals(FileAttachment fa)
    {
        return string.Equals(Filename, fa.Filename) && string.Equals(DisplayName, fa.DisplayName) && string.Equals(MimeType, fa.MimeType);
    }

    /// <summary>
    /// The HashCode for the FileAttachment.
    /// </summary>
    /// <returns>Returns the HashCode for the FileAttachment.</returns>
    /// <remarks>E.g. necessary for HashSet&lt;FileAttachment&gt;.</remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Filename != null ? Filename.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (MimeType != null ? MimeType.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
