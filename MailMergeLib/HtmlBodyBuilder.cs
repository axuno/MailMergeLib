using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;
using MimeKit.Utils;

namespace MailMergeLib
{
	/// <summary>
	/// Builds the HTML body part for a mail message. Images references will be converted to
	/// embedded cid content. Does not make any general changes to the HTML document
	/// </summary>
	internal class HtmlBodyBuilder : BodyBuilderBase
	{
		private readonly List<string> _badInlineFiles = new List<string>(10);
		private readonly List<FileAttachment> _inlineAtt = new List<FileAttachment>(20);
		private readonly HtmlTagHelper _tagHelper;
		private string _docBaseUrl = string.Empty;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="html">text of title tag to use</param>
		public HtmlBodyBuilder(string html)
		{
			BinaryTransferEncoding = ContentEncoding.Base64;

			_tagHelper = new HtmlTagHelper("base", html ?? string.Empty);
			if (_tagHelper.StartTags.Count <= 0) return;

			string href;
			if ((href = _tagHelper.GetAttributeValue(_tagHelper.StartTags[0], "href")) != null)
				_docBaseUrl = MakeUri(href);

			// remove if base tag is local file reference, because it's not usable in the resulting HTML
			if (href != null && href.StartsWith(Uri.UriSchemeFile))
				_tagHelper.ReplaceTag(_tagHelper.StartTags[0], string.Empty);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="html">HTML text</param>
		/// <param name="newTitle">text of title tag to use</param>
		public HtmlBodyBuilder(string html, string newTitle) : this(html)
		{
			_tagHelper.TagName = "title";
			if (_tagHelper.StartTagsTextEndTags.Count <= 0) return;

			// string oldTitle = _tagHelper.GetValueBetweenStartAndEndTag(_tagHelper.StartTagsTextEndTags[0]);
			_tagHelper.ReplaceTag(_tagHelper.StartTagsTextEndTags[0],
			                      _tagHelper.SetValueBetweenStartAndEndTag(_tagHelper.StartTagsTextEndTags[0], newTitle));
		}

		/// <summary>
		/// Gets the list of inline attachments (linked resources) referenced in the HTML text.
		/// </summary>
		public List<FileAttachment> InlineAtt
		{
			get { return _inlineAtt; }
		}


		/// <summary>
		/// Get the HTML representation of the source document
		/// </summary>
		public string DocHtml
		{
			get { return _tagHelper.HtmlText.ToString(); }
		}


		/// <summary>
		/// Gets the ready made body part for a mail message either 
		/// - as TextPart, if there are no inline attachments
		/// - as MultipartRelated with a TextPart and one or more MimeParts of type inline attachments
		/// </summary>
		public override MimeEntity GetBodyPart()
		{
			ReplaceImgSrcByCid();
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

			if (!_inlineAtt.Any())
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
			foreach (var ia in _inlineAtt)
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
					_badInlineFiles.Add(ia.Filename);
				}
				catch (IOException)
				{
					_badInlineFiles.Add(ia.Filename);
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
		public List<string> BadInlineFiles
		{
			get { return _badInlineFiles; }
		}

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
			var fileList = new List<string>();

			_tagHelper.TagName = "img";

			foreach (string element in _tagHelper.StartTags)
			{
				string srcAttr = _tagHelper.GetAttributeValue(element, "src");
				if (string.IsNullOrEmpty(srcAttr))
					continue;

				try
				{
					// this will succeed only with local files (at this time, they don't need to exist yet)
					string filename = MakeFullPath(MakeLocalPath(srcAttr));
					if (!fileList.Contains(filename))
					{
						var fileInfo = new FileInfo(filename);
						var contentType = MimeTypes.GetMimeType(filename);
						var cid = MimeUtils.GenerateMessageId();
						_inlineAtt.Add(new FileAttachment(filename, MakeCid(string.Empty, cid, new FileInfo(filename).Extension), contentType));
						_tagHelper.ReplaceTag(element, _tagHelper.SetAttributeValue(element, "src", MakeCid("cid:", cid, fileInfo.Extension)));
						fileList.Add(filename);
					}
				}
				catch
				{
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
			string fullpath = Tools.MakeFullPath(MakeLocalPath(_docBaseUrl), filename);
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
			       	? string.Format("{0}{1}/{2}", Uri.UriSchemeFile, Uri.SchemeDelimiter, localPath)
			       	: localPath;
		}
	}
}