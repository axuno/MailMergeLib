namespace MailMergeLib;

/// <summary>
/// Class for information about file attachments for mail messages created from strings.
/// </summary>
public class StringAttachment
{
    /// <summary>
    /// Creates a new string attachment for a <see cref="MailMergeMessage"/>
    /// </summary>
    public StringAttachment()
    {
        DisplayName = Content = string.Empty;
        MimeType = MimeKit.MimeTypes.GetMimeType( "application/octet-stream");
    }

    /// <summary>
    /// Creates a new string attachment for a <see cref="MailMergeMessage"/>
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
    /// Gets the name used in the attachment
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the MimeType as a string, like "text/plain"
    /// </summary>
    public string MimeType { get; private set; }

    #region *** Equality ***

    /// <summary>
    /// Determines whether the specified FileAttachment instances are equal.
    /// </summary>
    /// <remarks>E.g. necessary for HashSet&lt;FileAttachment&gt;.</remarks>
    /// <param name="sa"></param>
    /// <returns>Returns true, if both FileAttachments are equal, else false.</returns>
    public override bool Equals(object? sa)
    {
        if (sa is null) return false;
        if (ReferenceEquals(this, sa)) return true;
        if (sa.GetType() != GetType()) return false;
        return Equals((StringAttachment) sa);
    }

    private bool Equals(StringAttachment sa)
    {
        return string.Equals(Content, sa.Content) && string.Equals(DisplayName, sa.DisplayName) && string.Equals(MimeType, sa.MimeType);
    }

    /// <summary>
    /// The HashCode for the StringAttachment.
    /// </summary>
    /// <returns>Returns the HashCode for the StringAttachment.</returns>
    /// <remarks>E.g. necessary for HashSet&lt;StringAttachment&gt;.</remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Content != null ? Content.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (MimeType != null ? MimeType.GetHashCode() : 0);
            return hashCode;
        }
    }
}

#endregion
