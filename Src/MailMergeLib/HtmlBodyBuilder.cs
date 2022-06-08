using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MimeKit;
using MimeKit.Utils;

namespace MailMergeLib;

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
    private Uri _docBaseUri = new(string.Concat(UriScheme.File, UriScheme.SchemeDelimiter));
    private readonly object? _dataItem;
    private readonly string _defaultDocBaseUri = new Uri(string.Concat(UriScheme.File, UriScheme.SchemeDelimiter)).ToString();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="mailMergeMessage">The parent MailMergeMessage, where HtmlBodyBuilder processes HtmlText and Subject properties.</param>
    /// <param name="dataItem"></param>
    public HtmlBodyBuilder(MailMergeMessage mailMergeMessage, object? dataItem)
    {
        DocBaseUri = mailMergeMessage.Config.FileBaseDirectory;
        _mailMergeMessage = mailMergeMessage;
        _dataItem = dataItem;
        BinaryTransferEncoding = mailMergeMessage.Config.BinaryTransferEncoding;

        // Create a new parser front-end (can be re-used)
        var parser = new HtmlParser();
        //Just get the DOM representation
        _htmlDocument = parser.ParseDocument(mailMergeMessage.HtmlText);
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
        foreach (var element in _htmlDocument.All.Where(e => e is IHtmlScriptElement).ToList())
        {
            element.Remove();
        }

        // set the HTML title tag from email subject
        if (_htmlDocument.All.FirstOrDefault(m => m is IHtmlTitleElement) is IHtmlTitleElement titleEle)
        {
            titleEle.Text = _mailMergeMessage.SearchAndReplaceVars(_mailMergeMessage.Subject, _dataItem) ?? string.Empty;
        }

        // read the <base href="..."> tag in order to find the embedded image files later on
        var baseEle = _htmlDocument.All.FirstOrDefault(m => m is IHtmlBaseElement) as IHtmlBaseElement;
        var baseDir = baseEle?.Href == null ? null : new Uri(baseEle.Href);

        // only replace the base url if it was not set programmatically
        if (baseDir != null && _docBaseUri == new Uri(_defaultDocBaseUri))
        {
            _docBaseUri = baseDir;
        }

        // remove if base tag is local file reference, because it's not usable in the resulting HTML
        if (baseEle != null && baseDir != null && baseDir.Scheme == UriScheme.File)
            baseEle.Remove();
            
        ReplaceImgSrcByCid();

        // replace placeholders only in the HTML Body, because e.g. 
        // in the header there may be CSS definitions with curly brace which collide with SmartFormat {placeholders}
        if (_htmlDocument.Body != null)
            _htmlDocument.Body.InnerHtml =
                _mailMergeMessage.SearchAndReplaceVars(_htmlDocument.Body.InnerHtml, _dataItem) ?? string.Empty;

        var htmlTextPart = new TextPart("html")
        {
            ContentTransferEncoding = TextTransferEncoding
        };
        htmlTextPart.SetText(CharacterEncoding, DocHtml);  // MimeKit.ContentType.Charset is set using CharacterEncoding
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
        var mpr = new MultipartRelated(htmlTextPart);

        // Produce attachments as part of the multipart/related MIME part,
        // as described in RFC2387
        // Some older clients may need Inline Attachments instead of LinkedResources:
        // RFC2183: 2.1 The Inline Disposition Type
        // A body part should be marked `inline' if it is intended to be displayed automatically upon display of the message. Inline
        // body parts should be presented in the order in which they occur, subject to the normal semantics of multipart messages.
        foreach (var ia in InlineAtt)
        {
            try
            {
                var readyInlineAtt = new FileAttachment(_mailMergeMessage.SearchAndReplaceVarsInFilename(ia.Filename, _dataItem), _mailMergeMessage.SearchAndReplaceVars(ia.DisplayName, _dataItem));
                // create an inline image attachment for the file located at path
                var attachment = new AttachmentBuilder(readyInlineAtt, CharacterEncoding,
                    TextTransferEncoding, BinaryTransferEncoding).GetAttachment();
                attachment.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                attachment.ContentId = ia.DisplayName;
                attachment.FileName = null; // not needed for inline attachments, save some space

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
    /// Gets or sets the absolute local base URL of the HTML document. 
    /// This is used for building the path of embedded images.
    /// Can be an UNC path string (e.g. \\server\path), a local folder string (e.g. C:\user\x\document), or a URI string (e.g. file://c:/user/x/document)
    /// </summary>
    public string DocBaseUri
    {
        set => _docBaseUri = new Uri(string.IsNullOrEmpty(value)
            ? string.Concat(UriScheme.File, UriScheme.SchemeDelimiter)
            : value);
        get => _docBaseUri.ToString();
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
    /// <para>
    /// Note: Embedded images or existing (manually added) cid references will not be touched.
    /// </para>
    /// Example: &lt;img src="filename.jpg" /&lt; becomes &lt;img src="cid:unique-cid-jpg" /&lt;
    /// </summary>
    private void ReplaceImgSrcByCid()
    {
        var fileList = new Dictionary<string, string>();

        foreach (var element in _htmlDocument.All.Where(m => m is IHtmlImageElement))
        {
            var img = (IHtmlImageElement)element;
            var srcAttr = img.Attributes["src"];
            if (srcAttr is null) continue;

            var srcAttrValue = srcAttr.Value.Trim();
                
            // Skip embedded base64 image, or manually included cid reference
            if (string.IsNullOrEmpty(srcAttrValue) 
                || srcAttrValue.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)
                || srcAttrValue.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // replace any placeholders with variables
            srcAttrValue = _mailMergeMessage.SearchAndReplaceVars(srcAttrValue, _dataItem) ?? string.Empty;

            // Note: if srcAttrValue is a rooted path, _docBaseUrl will be ignored
            var srcUri = new Uri(_docBaseUri, srcAttrValue);

            // img src is not a local file 
            // so we just save the value with placeholders replaced
            if (srcUri.Scheme != UriScheme.File) 
            {
                // leave img.Attributes["src"].Value as it is
                continue;
            }

            // this will succeed only with local files (at this time, they don't need to exist yet)
            var filename = _mailMergeMessage.SearchAndReplaceVarsInFilename(srcUri.LocalPath, _dataItem);
            try
            {
                if (filename != null)
                {
                    if (!fileList.ContainsKey(filename))
                    {
                        var fileInfo = new FileInfo(filename);
                        var contentType = MimeTypes.GetMimeType(filename);
                        var cid = MimeUtils.GenerateMessageId();
                        InlineAtt.Add(new FileAttachment(fileInfo.FullName,
                            MakeCid(string.Empty, cid, fileInfo.Extension), contentType));
                        srcAttr.Value = MakeCid("cid:", cid, fileInfo.Extension);
                        fileList.Add(fileInfo.FullName, cid);
                    }
                    else
                    {
                        var cidForExistingFile = fileList[filename];
                        var fileInfo = new FileInfo(filename);
                        srcAttr.Value = MakeCid("cid:", cidForExistingFile, fileInfo.Extension);
                    }
                }
            }
            catch
            {
                BadInlineFiles.Add(filename ?? "(null)");
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
}