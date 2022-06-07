using System;
using System.IO;
using System.Text;
using MimeKit;

namespace MailMergeLib;

/// <summary>
/// Provides methods for building FileAttachments and StringAttachments for MailMergeMessages.
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
            Content = new MimeContent(File.OpenRead(fileAtt.Filename), ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            FileName = fileAtt.Filename,
            ContentType = {Name = displayName}
        };

        _attachment.ContentDisposition.FileName = fileAtt.DisplayName.Trim(new[] { '\\', '/', ':' });
        _attachment.ContentDisposition.CreationDate = File.GetCreationTime(fileAtt.Filename);
        _attachment.ContentDisposition.ModificationDate = File.GetLastWriteTime(fileAtt.Filename);
        _attachment.ContentDisposition.ReadDate = File.GetLastAccessTime(fileAtt.Filename);

        SetTextAndBinaryAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
    }

    public AttachmentBuilder(StringAttachment stringAtt, Encoding characterEncoding, ContentEncoding textTransferEncoding,
        ContentEncoding binaryTransferEncoding)
    {
        var displayName = ShortNameFromFile(stringAtt.DisplayName);

        var mimeTypeAndSubtype = stringAtt.MimeType.Split(new[] { '/' }, 2);
        _attachment = new MimePart(mimeTypeAndSubtype[0], mimeTypeAndSubtype[1])
        {
            Content = new MimeContent(
                new MemoryStream(characterEncoding.GetBytes(stringAtt.Content ?? string.Empty)),
                ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            FileName = displayName,
            ContentType = {MediaType = stringAtt.MimeType, Name = displayName}
        };

        _attachment.ContentDisposition.FileName = displayName;
        _attachment.ContentDisposition.CreationDate = DateTime.Now;
        _attachment.ContentDisposition.ModificationDate = DateTime.Now;
        _attachment.ContentDisposition.ReadDate = DateTime.Now;

        SetTextAndBinaryAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
    }

    public AttachmentBuilder(StreamAttachment streamAtt, Encoding characterEncoding, ContentEncoding textTransferEncoding,
        ContentEncoding binaryTransferEncoding)
    {
        var displayName = ShortNameFromFile(streamAtt.DisplayName);

        var mimeTypeAndSubtype = streamAtt.MimeType.Split(new[] { '/' }, 2);
        _attachment = new MimePart(mimeTypeAndSubtype[0], mimeTypeAndSubtype[1])
        {
            Content = new MimeContent(streamAtt.Stream, ContentEncoding.Default),
            ContentDisposition = new ContentDisposition(MimeKit.ContentDisposition.Attachment),
            ContentType = {Name = displayName},
        };

        _attachment.ContentDisposition.FileName = streamAtt.DisplayName.Trim(new[] { '\\', '/', ':' });
        _attachment.ContentDisposition.CreationDate = DateTime.Now;
        _attachment.ContentDisposition.ModificationDate = _attachment.ContentDisposition.CreationDate;
        _attachment.ContentDisposition.ReadDate = _attachment.ContentDisposition.CreationDate;

        SetTextAndBinaryAttachmentDefaults(characterEncoding, textTransferEncoding, binaryTransferEncoding);
    }

    public MimePart GetAttachment() => _attachment;

    internal static string ShortNameFromFile(string fileName)
    {
        var num = fileName.LastIndexOfAny(new[] { '\\', ':' }, fileName.Length - 1, fileName.Length);
        return num > 0 ? fileName.Substring(num + 1, (fileName.Length - num) - 1) : fileName;
    }

    private void SetTextAndBinaryAttachmentDefaults(Encoding characterEncoding, ContentEncoding textTransferEncoding, ContentEncoding binaryTransferEncoding)
    {
        if (_attachment.ContentType.MimeType.ToLower().StartsWith("text/"))
        {
            _attachment.ContentType.Charset = Tools.GetMimeCharset(characterEncoding);
            _attachment.ContentTransferEncoding = textTransferEncoding;
        }
        else
        {
            _attachment.ContentType.Charset = null;
            _attachment.ContentTransferEncoding = binaryTransferEncoding;
        }
    }
}