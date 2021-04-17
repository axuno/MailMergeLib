using MimeKit;
using MimeKit.Utils;

namespace MailMergeLib
{
    internal class PlainBodyBuilder : BodyBuilderBase
    {
        private readonly string _plainText;

        public PlainBodyBuilder(string plainText)
        {
            _plainText = plainText ?? string.Empty;
        }


        /// <summary>
        /// Gets the ready made body part for a mail message as TextPart
        /// </summary>
        public override MimeEntity GetBodyPart()
        {
            var plainTextPart = new TextPart("plain");
            plainTextPart.SetText(CharacterEncoding, _plainText);
            plainTextPart.ContentTransferEncoding = TextTransferEncoding;

            plainTextPart.ContentType.Charset = Tools.GetMimeCharset(CharacterEncoding); // RFC 2045 Section 5.1 - http://www.ietf.org;
            plainTextPart.ContentId = MimeUtils.GenerateMessageId();

            return plainTextPart;
        }
    }
}