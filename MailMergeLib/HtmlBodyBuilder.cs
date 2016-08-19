using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using MimeKit;
using MimeKit.Utils;

namespace MailMergeLib
{
	/// <summary>
	/// Builds the HTML body part for a mail message using AngleSharp. Image references will be converted to
	/// embedded cid content. Image references may include {Placeholders}.
	/// {Placeholders} in the HTML Body and will be replaced by variable values.
	/// </summary>
	/// <remarks>
	/// Removes any Script sections.
	/// </remarks>
	internal class HtmlBodyBuilder : BodyBuilderBase
	{
		private readonly MailMergeMessage _mailMergeMessage;
		private readonly IHtmlDocument _htmlDocument;
		private string _docBaseUrl;
		private object _dataItem;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="mailMergeMessage">The parent MailMergeMessage, where HtmlBodyBuilder processes HtmlText and Subject properties.</param>
		/// <param name="dataItem"></param>
		public HtmlBodyBuilder(MailMergeMessage mailMergeMessage, object dataItem)
		{
			_docBaseUrl = string.Empty;
			_mailMergeMessage = mailMergeMessage;
			_dataItem = dataItem;
			BinaryTransferEncoding = mailMergeMessage.BinaryTransferEncoding;

			// Create a new parser front-end (can be re-used)
			var parser = new HtmlParser();
			//Just get the DOM representation
			_htmlDocument = parser.Parse(mailMergeMessage.HtmlText);
		}

		/// <summary>
		/// Gets the list of inline attachments (linked resources) referenced in the HTML text.
		/// </summary>
		public HashSet<FileAttachment> InlineAtt { get; } = new HashSet<FileAttachment>();

		/// <summary>
		/// Get the HTML representation of the source document
		/// </summary>
		public string DocHtml => _htmlDocument.ToHtml();

		/// <summary>
		/// Gets the ready made body part for a mail message either 
		/// - as TextPart, if there are no inline attachments
		/// - as MultipartRelated with a TextPart and one or more MimeParts of type inline attachments
		/// </summary>
		public override MimeEntity GetBodyPart()
		{
			// remove all Script elements, because they cannot be used in mail messages
			foreach (var element in _htmlDocument.All.Where(e => e is IHtmlScriptElement))
			{
				element.Remove();
			}

			// set the HTML title tag from email subject
			var titleEle = _htmlDocument.All.FirstOrDefault(m => m is IHtmlTitleElement) as IHtmlTitleElement;
			if (titleEle != null)
			{
				titleEle.Text = _mailMergeMessage.SearchAndReplaceVars(_mailMergeMessage.Subject, _dataItem);
			}

			// read the <base href="..."> tag in order to find the embedded image files later on
			var baseEle = _htmlDocument.All.FirstOrDefault(m => m is IHtmlBaseElement) as IHtmlBaseElement;
			var baseDir = baseEle?.Href ?? string.Empty;
			_docBaseUrl = string.IsNullOrEmpty(baseDir) ? string.Empty : MakeUri(baseDir);

			// remove if base tag is local file reference, because it's not usable in the resulting HTML
			if (baseEle != null && baseDir.StartsWith(Uri.UriSchemeFile))
				baseEle.Remove();
			
			ReplaceImgSrcByCid();

			// replace placeholders only in the HTML Body, because e.g. 
			// in the header there may be CSS definitions with curly brace which collide with SmartFormat {placeholders}
			_htmlDocument.Body.InnerHtml = _mailMergeMessage.SearchAndReplaceVars(_htmlDocument.Body.InnerHtml, _dataItem);

			var htmlTextPart = new TextPart("html")
			{
				ContentTransferEncoding = Tools.IsSevenBit(DocHtml)
					? ContentEncoding.SevenBit
					: TextTransferEncoding != ContentEncoding.SevenBit
						? TextTransferEncoding
						: ContentEncoding.QuotedPrintable,

			};
			htmlTextPart.SetText(CharacterEncoding, DocHtml);
			htmlTextPart.ContentType.Charset = CharacterEncoding.HeaderName; // RFC 2045 Section 5.1 - http://www.ietf.org;
			htmlTextPart.ContentId = MimeUtils.GenerateMessageId();

			if (!InlineAtt.Any())
				return htmlTextPart;

			/*
				multipart/related
				text/html
				image/jpeg
				image/png
				image/gif...
			*/
			var mpr = new MultipartRelated {htmlTextPart};

			// Produce attachments as part of the multipart/related MIME part,
			// as described in RFC2387
			// Some older clients may need Inline Attachments instead of LinkedResources:
			// RFC2183: 2.1 The Inline Disposition Type
			// A bodypart should be marked `inline' if it is intended to be displayed automatically upon display of the message. Inline
			// bodyparts should be presented in the order in which they occur, subject to the normal semantics of multipart messages.
			foreach (var ia in InlineAtt)
			{
				try
				{
					// create an inline image attachment for the file located at path
					var attachment = new AttachmentBuilder(new FileAttachment(ia.Filename, ia.DisplayName, ia.MimeType), CharacterEncoding,
							TextTransferEncoding, BinaryTransferEncoding).GetAttachment();
					attachment.ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline);

					mpr.Add(attachment);
				}
				catch (FileNotFoundException)
				{
					BadInlineFiles.Add(ia.Filename);
				}
				catch (IOException)
				{
					BadInlineFiles.Add(ia.Filename);
				}
			}
			return mpr;
		}

		/// <summary>
		/// Gets or sets the Base URL of the HTML document. 
		/// This is used for building the path of embedded images.
		/// </summary>
		public string DocBaseUrl
		{
			set { _docBaseUrl = MakeUri(value); }
			get { return _docBaseUrl; }
		}

		/// <summary>
		/// Gets inline files referenced in the HTML text, that were missing or not readable.
		/// </summary>
		public HashSet<string> BadInlineFiles { get; } = new HashSet<string>();

		/// <summary>
		/// Gets or sets the transfer encoding for any binary content (e.g. Base64)
		/// </summary>
		public ContentEncoding BinaryTransferEncoding { get; set; }

		/// <summary>
		/// Converts the SRC attribute of IMG tags into embedded content ids (cid).
		/// Example: &lt;img src="filename.jpg" /&lt; becomes &lt;img src="cid:unique-cid-jpg" /&lt;
		/// </summary>
		private void ReplaceImgSrcByCid()
		{
			var fileList = new HashSet<string>();

			foreach (var element in _htmlDocument.All.Where(m => m is IHtmlImageElement))
			{
				var img = (IHtmlImageElement)element;
				var currSrc = img.Attributes["src"]?.Value;
				if (currSrc == null) continue;

				// replace any placeholders with variables
				currSrc = _mailMergeMessage.SearchAndReplaceVars(currSrc, _dataItem);
				// this will succeed only with local files (at this time, they don't need to exist yet)
				var filename = MakeFullPath(MakeLocalPath(currSrc));
				try
				{
					if (!fileList.Contains(filename))
					{
						var fileInfo = new FileInfo(filename);
						var contentType = MimeTypes.GetMimeType(filename);
						var cid = MimeUtils.GenerateMessageId();
						InlineAtt.Add(new FileAttachment(filename, MakeCid(string.Empty, cid, new FileInfo(filename).Extension), contentType));

						img.Attributes["src"].Value = MakeCid("cid:", cid, fileInfo.Extension);
						fileList.Add(filename);
					}
				}
				catch
				{
					BadInlineFiles.Add(filename);
					continue;
				}
			}
		}

		/// <summary>
		/// Makes the content identifier (CID)
		/// </summary>
		/// <param name="prefix">i.e. normally "cid:"</param>
		/// <param name="contentId">unique indentifier</param>
		/// <param name="fileExt">file extension, so that content type can be easily identified. May be string.empty</param>
		/// <returns></returns>
		private static string MakeCid(string prefix, string contentId, string fileExt)
		{
			return prefix + contentId + fileExt.Replace('.', '-');
		}

		/// <summary>
		/// Determines the full path for the given local file
		/// </summary>
		/// <param name="filename">local file name</param>
		/// <returns>Full path for the given local file</returns>
		private string MakeFullPath(string filename)
		{
			var fullpath = Tools.MakeFullPath(MakeLocalPath(_docBaseUrl), filename);
			return fullpath;
		}

		/// <summary>
		/// Determines the local path for the given URI
		/// </summary>
		/// <param name="uri">RFC1738: "file://" [ host | "localhost" ] "/" path</param>
		/// <returns>Local path for the given URI</returns>
		private static string MakeLocalPath(string uri)
		{
			return uri.StartsWith(Uri.UriSchemeFile) ? new Uri(uri).LocalPath : uri;

			/* Note:
			 * In case the filename does not contain the Uri.UriSchemeFile prefix,
			 * it will not be decoded. Then the follwing line should be used.

			 * Pre-process for + sign space formatting since System.Uri doesn't handle it.
			 * "Plus" literals are encoded as %2b so this should be safe enough:
			 * 
			 * return uri.StartsWith(Uri.UriSchemeFile) ? new Uri(uri).LocalPath : Uri.UnescapeDataString(uri.Replace("+", " "));
			 */
		}

		/// <summary>
		/// Makes a RFC1738 compliant URI, like: "file://" [ host | "localhost" ] "/" path
		/// </summary>
		/// <param name="localPath"></param>
		/// <returns>A RFC1738 compliant URI: "file://" [ host | "localhost" ] "/" path</returns>
		private static string MakeUri(string localPath)
		{
			return ! localPath.StartsWith(Uri.UriSchemeFile)
			    ? $"{Uri.UriSchemeFile}{Uri.SchemeDelimiter}/{localPath}"
				: localPath;
		}
	}
}