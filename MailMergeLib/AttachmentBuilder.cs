using System;
using System.IO;
using System.Text;
using MimeKit;

namespace MailMergeLib
{
	/// <summary>
	/// Provides methods for building FileAttachments and StringAttachmets for MailMergeMessages.
	/// </summary>
	internal class AttachmentBuilder
	{
		private readonly MimePart _attachment;

		public AttachmentBuilder(FileAttachment fileAtt, Encoding characterEncoding, ContentEncoding textTransferEncoding,
								 ContentEncoding binaryTransferEncoding)
		{
			var displayName = ShortNameFromFile(fileAtt.DisplayName);

			var mimeTypeAndSubtype = fileAtt.MimeType.Split(new[] { '/' }, 2);
			_attachment = new MimePart(mimeTypeAndSubtype[0], mimeTypeAndSubtype[1])
			{
				ContentObject = new ContentObject(File.OpenRead(fileAtt.Filename), ContentEncoding.Default),
				ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Attachment),
				FileName = fileAtt.Filename
			};

			_attachment.ContentType.Name = displayName;
			_attachment.ContentDisposition.FileName = fileAtt.DisplayName.Trim(new[] {'\\', '/', ':'});
			_attachment.ContentDisposition.CreationDate = File.GetCreationTime(fileAtt.Filename);
			_attachment.ContentDisposition.ModificationDate = File.GetLastWriteTime(fileAtt.Filename);
			_attachment.ContentDisposition.ReadDate = File.GetLastAccessTime(fileAtt.Filename);
			
			SetTextAndBinarayAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
		}

		public AttachmentBuilder(StringAttachment stringAtt, Encoding characterEncoding, ContentEncoding textTransferEncoding,
								 ContentEncoding binaryTransferEncoding)
		{
			var displayName = ShortNameFromFile(stringAtt.DisplayName);

			var mimeTypeAndSubtype = stringAtt.MimeType.Split(new[] { '/' }, 2);
			_attachment = new MimePart(mimeTypeAndSubtype[0], mimeTypeAndSubtype[1])
			{
				ContentObject = new ContentObject(new MemoryStream(characterEncoding.GetBytes(stringAtt.Content ?? string.Empty)), ContentEncoding.Default),
				ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Attachment),
				FileName = displayName
			};

			_attachment.ContentType.MediaType = stringAtt.MimeType;
			_attachment.ContentType.Name = displayName;
			_attachment.ContentDisposition.FileName = displayName;
			_attachment.ContentDisposition.CreationDate = DateTime.Now;
			_attachment.ContentDisposition.ModificationDate = DateTime.Now;
			_attachment.ContentDisposition.ReadDate = DateTime.Now;

			SetTextAndBinarayAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
		}

		public AttachmentBuilder(StreamAttachment streamAtt, Encoding characterEncoding, ContentEncoding textTransferEncoding,
								 ContentEncoding binaryTransferEncoding)
		{
			var displayName = ShortNameFromFile(streamAtt.DisplayName);

			var mimeTypeAndSubtype = streamAtt.MimeType.Split(new[] { '/' }, 2);
			_attachment = new MimePart(mimeTypeAndSubtype[0], mimeTypeAndSubtype[1])
			{
				ContentObject = new ContentObject(streamAtt.Stream, ContentEncoding.Default),
				ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Attachment),
			};

			_attachment.ContentType.Name = displayName;
			_attachment.ContentDisposition.FileName = streamAtt.DisplayName.Trim(new[] {'\\', '/', ':'});
			_attachment.ContentDisposition.CreationDate = DateTime.Now;
			_attachment.ContentDisposition.ModificationDate = _attachment.ContentDisposition.CreationDate;
			_attachment.ContentDisposition.ReadDate = _attachment.ContentDisposition.CreationDate;
			
			SetTextAndBinarayAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
		}

		public MimePart GetAttachment() => _attachment;

		internal static string ShortNameFromFile(string fileName)
		{
			var num = fileName.LastIndexOfAny(new[] {'\\', ':'}, fileName.Length - 1, fileName.Length);
			return num > 0 ? fileName.Substring(num + 1, (fileName.Length - num) - 1) : fileName;
		}

		private void SetTextAndBinarayAttachmentDefaults(Encoding characterEncoding, ContentEncoding textTransferEncoding, ContentEncoding binaryTransferEncoding)
		{ 
			if (_attachment.ContentType.MimeType.ToLower().StartsWith("text/"))
			{
				_attachment.ContentType.Charset = Tools.GetMimeCharset(characterEncoding);
				_attachment.ContentTransferEncoding = Tools.IsSevenBit(_attachment.ContentObject.Stream, characterEncoding)
												   ? ContentEncoding.SevenBit
												   : textTransferEncoding;
			}
			else
			{
				_attachment.ContentType.Charset = null;
				_attachment.ContentTransferEncoding = binaryTransferEncoding;
			}
		}
	}
}