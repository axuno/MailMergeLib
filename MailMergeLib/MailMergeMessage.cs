using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MailKit;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MimeKit;

namespace MailMergeLib
{
	/// <summary>
	/// Represents an email message that can be sent using the MailMergeLib.MailMergeSender class.
	/// </summary>
	partial class MailMergeMessage : IDisposable
	{
		#region *** Private content fields ***

		private MimeEntity _textMessagePart;  // plain text and/or html text, maybe with inline attachments
		private List<MimePart> _attachmentParts;
		private static readonly object _syncRoot = new object();

		#endregion

		#region *** Private fields for Encoding and Globalization ***

		#endregion

		#region *** Private lists for tracking errors ***

		private readonly HashSet<string> _badAttachmentFiles = new HashSet<string>();
		private readonly HashSet<string> _badMailAddr = new HashSet<string>();
		private HashSet<string> _badInlineFiles = new HashSet<string>();
		private HashSet<string> _badVariableNames = new HashSet<string>();

		#endregion

		#region *** Private fields for Attachments ***

		private readonly List<FileAttachment> _inlineAttExternal = new List<FileAttachment>();

		#endregion

		#region *** Private mail header constants ***

		// special mail headers
		private const string CConfirmReading = "x-confirm-reading-to";
		

		#endregion

		#region *** Constructor ***

		/// <summary>
		/// Creates an empty mail merge message.
		/// </summary>
		public MailMergeMessage()
		{
			IgnoreIllegalRecipientAddr = true;
			DeliveryStatusNotification = DeliveryStatusNotification.Never;
			Priority = MessagePriority.Normal;
			Xmailer = null;
			Headers = new NameValueCollection();
			BinaryTransferEncoding = ContentEncoding.Base64;
			TextTransferEncoding = ContentEncoding.SevenBit;
			CharacterEncoding = Encoding.Default;

			SmartFormatter.ErrorAction = ErrorAction.Ignore;
			SmartFormatter.Parser.ErrorAction = ErrorAction.ThrowError;
			// Smart.Format("{Name:choose(null|):N/A|empty|{Name}}", variable), where abc.Name NULL, string.Emtpy or a string

			MailMergeMessage msg = this;
			MailMergeAddresses = new MailMergeAddressCollection(ref msg);
			
			FileBaseDir = Environment.CurrentDirectory;
		}

		/// <summary>
		/// Creates a new mail merge message.
		/// </summary>
		/// <param name="subject">Mail message subject.</param>
		public MailMergeMessage(string subject)
			: this()
		{
			Subject = subject;
		}

		/// <summary>
		/// Creates a new mail merge message.
		/// </summary>
		/// <param name="subject">Mail message subject.</param>
		/// <param name="plainText">Plain text of the mail message.</param>
		public MailMergeMessage(string subject, string plainText)
			: this(subject)
		{
			PlainText = plainText;
			HtmlText = string.Empty;
		}

		/// <summary>
		/// Creates a new mail merge message.
		/// </summary>
		/// <param name="subject">Mail message subject.</param>
		/// <param name="plainText">Plain text part of the mail message.</param>
		/// <param name="htmlText">HTML message part of the mail message.</param>
		public MailMergeMessage(string subject, string plainText, string htmlText)
			: this(subject, plainText)
		{
			HtmlText = htmlText;
		}

		/// <summary>
		/// Creates a new mail merge message.
		/// </summary>
		/// <param name="subject">Mail message subject.</param>
		/// <param name="plainText">Plain text part of the mail message.</param>
		/// <param name="fileAtt">File attachments of the mail message.</param>
		public MailMergeMessage(string subject, string plainText, HashSet<FileAttachment> fileAtt)
			: this(subject, plainText, string.Empty, fileAtt)
		{
		}

		/// <summary>
		/// Creates a new mail merge message.
		/// </summary>
		/// <param name="subject">Mail message subject.</param>
		/// <param name="plainText">Plain text part of the mail message.</param>
		/// <param name="htmlText">HTML message part of the mail message.</param>
		/// <param name="fileAtt">File attachments of the mail message.</param>
		public MailMergeMessage(string subject, string plainText, string htmlText, HashSet<FileAttachment> fileAtt)
			: this(subject, plainText, htmlText)
		{
			FileAttachments = fileAtt;
		}

		#endregion

		
		/// <summary>
		/// Gets or sets the mail message subject.
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		/// Gets or sets the mail message plain text content.
		/// </summary>
		public string PlainText { get; set; }

		/// <summary>
		/// Gets or sets the mail message HTML content.
		/// </summary>
		public string HtmlText { get; set; }

		/// <summary>
		/// Gets or sets the data source the TextVariableManager binds to.
		/// </summary>

		/// <summary>
		/// Gets or sets the encoding to be used for any text content (plain text and/or HTML)
		/// </summary>
		public Encoding CharacterEncoding { get; set; }

		/// <summary>
		/// Gets or sets the transfer encoding for any text (e.g. SevenBit)
		/// </summary>
		public ContentEncoding TextTransferEncoding { get; set; }

		/// <summary>
		/// Gets or sets the transfer encoding for any binary content (e.g. Base64)
		/// </summary>
		public ContentEncoding BinaryTransferEncoding { get; set; }

		/// <summary>
		/// Gets or sets the culture info to apply for any variable formatting (like date, time etc.).
		/// Default: CultureInfo of the current thread.
		/// </summary>
		public CultureInfo CultureInfo { get; set; } = Thread.CurrentThread.CurrentCulture;

		/// <summary>
		/// Gets or sets the instance of the MailSmartFormatter (derived from SmartFormat.NET's SmartFormatter) which will be used with MailMergeLib.
		/// </summary>
		public MailSmartFormatter SmartFormatter { get; set; } = new MailSmartFormatter(ErrorAction.ThrowError, ErrorAction.Ignore);
		
		/// <summary>
		/// Converts the HtmlText property into plain text (without tags or html entities)
		/// If the converter is null, the ParsingHtmlConverter will be used. If this fails,
		/// a simple RegExHtmlConverter will be used.
		/// </summary>
		/// <param name="converter">
		/// The IHtmlConverter to be used for converting. If the converter is null, the 
		/// ParsingHtmlConverter will be used. If this fails,  RegExHtmlConverter will be 
		/// used. Usage of a parsing converter is recommended.
		/// </param>
		/// <returns>Returns the plain text representation of the HTML string.</returns>
		public string ConvertHtmlToPlainText(IHtmlConverter converter = null)
		{
			try
			{
				return converter == null
				       	? (new AngleSharpHtmlConverter()).ToPlainText(HtmlText)
				       	: converter.ToPlainText(HtmlText);
			}
			catch (FileNotFoundException)
			{
				// AngleSharp.dll not found
				return (new RegExHtmlConverter()).ToPlainText(HtmlText);
			}
		}

		/// <summary>
		/// Gets the MimeMessage representation of the MailMergeMessage for a specific data item.
		/// </summary>
		/// <param name="dataItem">
		/// The following types are accepted:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, any other class instances, anonymous types, and null.
		/// For class instances it's allowed to use the name of parameterless methods; use method names WITHOUT parentheses.
		/// </param>
		/// <returns>Returns a MailMessage ready to be sent by an SmtpClient.</returns>
		/// <exception cref="MailMergeMessageException">Throws a general MailMergeMessageException, which contains a list of exceptions giving more details.</exception>
		public MimeMessage GetMimeMessage(object dataItem = default(object))
		{
			lock (_syncRoot)
			{
				var mimeMessage = new MimeMessage();
				AddSubjectToMailMessage(mimeMessage, dataItem);
				AddAttributesToMailMessage(mimeMessage, dataItem);
				AddAddressesToMailMessage(mimeMessage, dataItem);

				BuildTextMessagePart(dataItem);
				BuildAttachmentPartsForMessage(dataItem);

				var exceptions = new List<Exception>();

				if (mimeMessage.To.Count == 0 && mimeMessage.Cc.Count == 0 && mimeMessage.Bcc.Count == 0)
					exceptions.Add(new AddressException("No recipients.", _badMailAddr, null));
				if (string.IsNullOrWhiteSpace(mimeMessage.From.ToString()))
					exceptions.Add(new AddressException("No from address.", _badMailAddr, null));
				if (HtmlText.Length == 0 && PlainText.Length == 0 && Subject.Length == 0 && !FileAttachments.Any() &&
				    !InlineAttachments.Any() && !StringAttachments.Any() && !StreamAttachments.Any())
					exceptions.Add(new EmtpyContentException("Message is empty.", null));
				if (_badMailAddr.Count > 0)
					exceptions.Add(
						new AddressException($"Bad mail address(es): {string.Join(", ", _badMailAddr.ToArray())}",
							_badMailAddr, null));
				if (_badInlineFiles.Count > 0)
					exceptions.Add(
						new AttachmentException(
							$"Inline attachment(s) missing or not readable: {string.Join(", ", _badInlineFiles.ToArray())}",
							_badInlineFiles, null));
				if (_badAttachmentFiles.Count > 0)
					exceptions.Add(
						new AttachmentException(
							$"File attachment(s) missing or not readable: {string.Join(", ", _badAttachmentFiles.ToArray())}",
							_badAttachmentFiles, null));
				if (_badVariableNames.Count > 0)
					exceptions.Add(
						new VariableException(
							$"Variable(s) for placeholder(s) not found: {string.Join(", ", _badVariableNames.ToArray())}",
							_badVariableNames, null));

				// Finally throw general exception
				if (exceptions.Count > 0)
					throw new MailMergeMessageException("Building of message failed with one or more exceptions.", exceptions, mimeMessage);

				if (_attachmentParts.Any())
				{
					var mixed = new Multipart("mixed");

					if (_textMessagePart != null)
						mixed.Add(_textMessagePart);

					foreach (var att in _attachmentParts)
					{
						mixed.Add(att);
					}

					mimeMessage.Body = mixed;
				}

				if (mimeMessage.Body == null)
				{
					mimeMessage.Body = _textMessagePart ?? new TextPart("plain") {Text = string.Empty};
				}

				return mimeMessage;
			}
		}

		#region *** Destructor and IDisposable Members ***

		private bool _disposed;

		/// <summary>
		/// Destructor.
		/// </summary>
		~MailMergeMessage()
		{
			Dispose(false);
		}

		/// <summary>
		/// Dispose MailMergeMessage
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_textMessagePart = null;
					_attachmentParts = null;
				}
			}
			_disposed = true;
		}

		#endregion

		#region *** Content methods and properties ***

		/// <summary>
		/// Gets or sets files that will be attached to a mail message.
		/// File names may contain placeholders.
		/// </summary>
		public HashSet<FileAttachment> FileAttachments { get; set; } = new HashSet<FileAttachment>();

		/// <summary>
		/// Gets or sets streams that will be attached to a mail message.
		/// </summary>
		public List<StreamAttachment> StreamAttachments { get; set; } = new List<StreamAttachment>();

		/// <summary>
		/// Gets inline attachments (linked resources of the HTML body) of a mail message.
		/// They are generated automatically with all image sources pointing to local files.
		/// </summary>
		public HashSet<FileAttachment> InlineAttachments { get; private set; } = new HashSet<FileAttachment>();

		/// <summary>
		/// Gets or sets string attachments that will be attached to a mail message.
		/// String attachments can be text or binary.
		/// </summary>
		public List<StringAttachment> StringAttachments { get; set; } = new List<StringAttachment>();

		/// <summary>
		/// Gets or sets the local base directory of HTML content.
		/// It useful for retrieval of inline attachments (linked resources of the HTML body).
		/// </summary>
		public string FileBaseDir { get; set; }

		/// <summary>
		/// Replaces all variables in the text with their corresponding values.
		/// Used for subject, body and attachment.
		/// </summary>
		/// <param name="text">Text to search and replace.</param>
		/// <param name="dataItem"></param>
		/// <returns>Returns the text with all variables replaced.</returns>
		internal string SearchAndReplaceVars(string text, object dataItem)
		{
			return SmartFormatter.Format(CultureInfo, text, dataItem);
		}

		/// <summary>
		/// Adds external inline attachments (linked resources of the HTML body) of a mail message.
		/// They are normally generated automatically with all image sources pointing to local files,
		/// but with this method such files can be added as well.
		/// </summary>
		/// <param name="att"></param>
		public void AddExternalInlineAttachment(FileAttachment att)
		{
			_inlineAttExternal.Add(att);
		}

		/// <summary>
		/// Clears external inline attachments (linked resources of the HTML body) of a mail message.
		/// They are normally generated automatically with all image sources pointing to local files.
		/// This method only removes attachments formerly added with AddExternalInlineAttachment.
		/// </summary>
		public void ClearExternalInlineAttachment()
		{
			_inlineAttExternal.Clear();
		}


		/// <summary>
		/// Prepares the mail message subject:
		/// Replacing placeholders with their values and setting correct encoding.
		/// </summary>
		private void AddSubjectToMailMessage(MimeMessage msg, object dataItem)
		{
			var subject = SearchAndReplaceVars(Subject, dataItem);
			msg.Subject = subject;
			msg.Headers.Add(HeaderId.Subject, CharacterEncoding, subject);
		}


		/// <summary>
		/// Prepares the mail message part (plain text and/or HTML:
		/// Replacing placeholders with their values and setting correct encoding.
		/// </summary>
		private void BuildTextMessagePart(object dataIteam)
		{
			_badInlineFiles.Clear();
			_textMessagePart = null;
			
			MultipartAlternative alternative = null;

			// create the plain text body part
			TextPart plainTextPart = null;

			if (!string.IsNullOrEmpty(PlainText))
			{
				var plainText = SearchAndReplaceVars(PlainText, dataIteam);
				plainTextPart = (TextPart) new PlainBodyBuilder(plainText)
				{
					TextTransferEncoding = TextTransferEncoding,
					CharacterEncoding = CharacterEncoding
				}.GetBodyPart();
				
				if (!string.IsNullOrEmpty(HtmlText))
				{
					// there is plain text AND html text
					alternative = new MultipartAlternative { plainTextPart };
					_textMessagePart = alternative;
				}
				else
				{
					// there is only a plain text part, which could even be null
					_textMessagePart = plainTextPart;
				}
			}

			if (!string.IsNullOrEmpty(HtmlText))
			{
				// create the HTML text body part with any linked resources
				// replacing any placeholders in the text or files with variable values
				var htmlBody = new HtmlBodyBuilder(this, dataIteam)
				{
					DocBaseUrl = FileBaseDir,
					TextTransferEncoding = TextTransferEncoding,
					BinaryTransferEncoding = BinaryTransferEncoding,
					CharacterEncoding = CharacterEncoding
				};

				_inlineAttExternal.ForEach(ia => htmlBody.InlineAtt.Add(ia));
				InlineAttachments = htmlBody.InlineAtt;
				htmlBody.BadInlineFiles.ToList().ForEach(f => _badInlineFiles.Add(f));

				if (alternative != null)
				{
					alternative.Add(htmlBody.GetBodyPart());
					_textMessagePart = alternative;
				}
				else
				{
					_textMessagePart = htmlBody.GetBodyPart();
				}
			}
			else
			{
				InlineAttachments = new HashSet<FileAttachment>();
				_badInlineFiles = new HashSet<string>();
			}
		}


		/// <summary>
		/// Prepares the mail message file and string attachments:
		/// Replacing placeholders with their values and setting correct encoding.
		/// </summary>
		private void BuildAttachmentPartsForMessage(object dataItem)
		{
			_badAttachmentFiles.Clear();
			_attachmentParts = new List<MimePart>();

			foreach (var fa in FileAttachments)
			{
				var filename = MakeFullPath(SearchAndReplaceVars(fa.Filename, dataItem));
				var displayName = SearchAndReplaceVars(fa.DisplayName, dataItem);

				try
				{
					_attachmentParts.Add(
						new AttachmentBuilder(new FileAttachment(filename, displayName, fa.MimeType), CharacterEncoding,
							TextTransferEncoding, BinaryTransferEncoding).GetAttachment());
				}
				catch (FileNotFoundException)
				{
					_badAttachmentFiles.Add(filename);
				}
				catch (IOException)
				{
					_badAttachmentFiles.Add(filename);
				}
			}

			foreach (var sa in StreamAttachments)
			{
				var displayName = SearchAndReplaceVars(sa.DisplayName, dataItem);
				_attachmentParts.Add(
					new AttachmentBuilder(new StreamAttachment(sa.Stream, displayName, sa.MimeType), CharacterEncoding,
						TextTransferEncoding, BinaryTransferEncoding).GetAttachment());
			}

			foreach (var sa in StringAttachments)
			{
				var displayName = SearchAndReplaceVars(sa.DisplayName, dataItem);
				_attachmentParts.Add(
					new AttachmentBuilder(new StringAttachment(sa.Content, displayName, sa.MimeType), CharacterEncoding,
						TextTransferEncoding, BinaryTransferEncoding).GetAttachment());
			}
		}


		/// <summary>
		/// Calculates the full path of the file name, using the base directory if set.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns>The full path of the file.</returns>
		private string MakeFullPath(string filename)
		{
			return Tools.MakeFullPath(FileBaseDir, filename);
		}

		#endregion

		#region *** Address methods and properties ***

		/// <summary>
		/// Gets the collection of recipients and sender addresses of the message.
		/// </summary>
		public MailMergeAddressCollection MailMergeAddresses { get; private set; }


		/// <summary>
		/// If true, empty merge recipient addresses will be skipped.
		/// If false, empty addresses will throw an exception.
		/// </summary>
		public bool IgnoreIllegalRecipientAddr { get; set; }


		/// <summary>
		/// Prepares all recipient address and the corresponding header fields of a mail message.
		/// </summary>
		private void AddAddressesToMailMessage(MimeMessage mimeMessage, object dataItem)
		{
			#region *** Clear MailMessage headers ***

			/*
			 * Not really necessary because we always work on a NEW instance of a MailMessage
			 */

			//cc, _bcc, _sender, _from, _replyto;
			mimeMessage.To.Clear();
			mimeMessage.Cc.Clear();
			mimeMessage.Bcc.Clear();
			mimeMessage.ReplyTo.Clear();
			mimeMessage.Sender = null;

			#endregion

			_badMailAddr.Clear();

			MailMergeAddress testAddress = null;
			foreach (MailMergeAddress mmAddr in MailMergeAddresses.Where(mmAddr => mmAddr.AddrType == MailAddressType.TestAddress))
			{
				testAddress = new MailMergeAddress(MailAddressType.TestAddress, mmAddr.Address, mmAddr.DisplayName,
				                                   mmAddr.DisplayNameCharacterEncoding);
			}

			foreach (var mmAddr in MailMergeAddresses)
			{
				try
				{
					MailboxAddress mailboxAddr;
					// use the address part the test mail address (if set) but use the original display name
					if (testAddress != null)
					{
						testAddress.DisplayName = mmAddr.DisplayName;
						mailboxAddr = testAddress.GetMailAddress(SmartFormatter, dataItem);
					}
					else
					{
						mailboxAddr = mmAddr.GetMailAddress(SmartFormatter, dataItem);
					}

					SmartFormatter.MissingVariables.ToList().ForEach(f => _badVariableNames.Add(f));

					if (IgnoreIllegalRecipientAddr && mailboxAddr == null)
						continue;
					
					switch (mmAddr.AddrType)
					{
						case MailAddressType.To:
							mimeMessage.To.Add(mailboxAddr);
							break;
						case MailAddressType.CC:
							mimeMessage.Cc.Add(mailboxAddr);
							break;
						case MailAddressType.Bcc:
							mimeMessage.Bcc.Add(mailboxAddr);
							break;
						case MailAddressType.ReplyTo:
							mimeMessage.ReplyTo.Add(mailboxAddr);
							break;
						case MailAddressType.ConfirmReadingTo:
							mimeMessage.Headers.Remove(HeaderId.Received);
							mimeMessage.Headers.Remove(HeaderId.DispositionNotificationTo);
							mimeMessage.Headers.Add(CConfirmReading, mailboxAddr.Address);
							mimeMessage.Headers.Add(HeaderId.DispositionNotificationTo, mailboxAddr.Address);
							break;
						case MailAddressType.ReturnReceiptTo:
							mimeMessage.Headers.Remove(HeaderId.ReturnReceiptTo);
							mimeMessage.Headers.Add(HeaderId.ReturnReceiptTo, mailboxAddr.Address);
							break;
						case MailAddressType.Sender:
							mimeMessage.Sender = mailboxAddr;
							break;
						case MailAddressType.From:
							mimeMessage.From.Add(mailboxAddr);
							break;
					}
				}
				catch (FormatException)
				{
					_badMailAddr.Add(mmAddr.ToString());
				}
			}
		}

		#endregion

		#region *** Special attributes related properties and methods ***

		/// <summary>
		/// Gets or sets the user defined headers of a mail message.
		/// </summary>
		public NameValueCollection Headers { get; set; }

		/// <summary>
		/// Gets or sets the "x-mailer" header value to be used.
		/// </summary>
		public string Xmailer { get; set; }

		/// <summary>
		/// Gets or sets the priority of a mail message.
		/// </summary>
		[CLSCompliant(false)]
		public MessagePriority Priority { get; set; }

		/// <summary>
		/// Gets or sets the delivery notification options, which will be used by SmtpClient()
		/// </summary>
		[CLSCompliant(false)]
		public DeliveryStatusNotification DeliveryStatusNotification { get; set; }

		/// <summary>
		/// Sets all attributes of a mail message.
		/// </summary>
		private void AddAttributesToMailMessage(MimeMessage mimeMessage, object dataItem)
		{
			mimeMessage.Priority = Priority;
			
			if (!string.IsNullOrEmpty(Xmailer))
			{
				mimeMessage.Headers.Add(HeaderId.XMailer, Xmailer);
			}
		}

		#endregion
	}
}