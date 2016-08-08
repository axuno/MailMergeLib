namespace MailMergeLib
{
	/// <summary>
	/// Class for information about file attachments for mail messages
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
		/// <param name="displayName">Name and extension as the reader of the mail should see it (in case of inline attachments displayName is used for CIDs</param>
		public FileAttachment(string fileName, string displayName)
		{
			Filename = fileName;
			DisplayName = displayName;
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
	}
}