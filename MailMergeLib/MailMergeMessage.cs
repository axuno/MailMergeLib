using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MailMergeLib.MessageStore;
using MailMergeLib.Serialization;
using SmartFormat.Extensions;
using MailMergeLib.Templates;
using MimeKit;
using YAXLib;
#if NETFRAMEWORK || NETSTANDARD2_0
using System.Data;
#endif

namespace MailMergeLib
{
    /// <summary>
    /// Represents an email message that can be sent using the MailMergeLib.MailMergeSender class.
    /// </summary>
    [YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly, Options = YAXSerializationOptions.DontSerializeNullObjects)]
    public partial class MailMergeMessage : IDisposable
    {
        #region *** Private fields ***

        private MimeEntity _textMessagePart;  // plain text and/or html text, maybe with inline attachments
        private List<MimePart> _attachmentParts;

        // backing fields for properties are necessary for private setters used in deserialization!
        private string _subject = string.Empty;
        private string _plainText = string.Empty;
        private string _htmlText = string.Empty;
        private MailMergeAddressCollection _mailMergeAddresses;
        private HashSet<FileAttachment> _fileAttachments = new HashSet<FileAttachment>();
        private List<StreamAttachment> _streamAttachments = new List<StreamAttachment>();
        private HashSet<FileAttachment> _inlineAttachments = new HashSet<FileAttachment>();
        private HashSet<StringAttachment> _stringAttachments = new HashSet<StringAttachment>();
        private HashSet<FileAttachment> _externalInlineAttachments = new HashSet<FileAttachment>();
        private HeaderList _headers = new HeaderList();
        private MessageInfo _info = new MessageInfo();
        private MessageConfig _config = new MessageConfig();
        private Templates.Templates _templates = new MailMergeLib.Templates.Templates();

        // disposal and sync
        private bool _disposed;
        private static readonly object SyncRoot = new object();

        #endregion

        #region *** Private lists for tracking errors when generating a MimeMessage ***

        private readonly HashSet<string> _badAttachmentFiles = new HashSet<string>();
        private readonly HashSet<string> _badMailAddr = new HashSet<string>();
        private readonly HashSet<string> _badInlineFiles = new HashSet<string>();
        private readonly HashSet<string> _badVariableNames = new HashSet<string>();
        private readonly List<ParseException> _parseExceptions = new List<ParseException>();

        #endregion

        #region *** Constructor ***

        /// <summary>
        /// Creates an empty mail merge message.
        /// </summary>
        public MailMergeMessage()
        {
            Config.IgnoreIllegalRecipientAddresses = true;
            Config.Priority = MessagePriority.Normal;
            SmartFormatter = GetConfiguredMailSmartFormatter();
            Config.SmartFormatterConfig.OnConfigChanged += SmartFormatter.SetConfig;
            _mailMergeAddresses = new MailMergeAddressCollection(this);
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
        public MailMergeMessage(string subject, string plainText, IEnumerable<FileAttachment> fileAtt)
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
        public MailMergeMessage(string subject, string plainText, string htmlText, IEnumerable<FileAttachment> fileAtt)
            : this(subject, plainText, htmlText)
        {
            fileAtt.ToList().ForEach(fa => FileAttachments.Add(fa));
        }

        #endregion

        #region *** Info, addresses and headers ***

        /// <summary>
        /// Information about the <c>MailMergeMessage</c>
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public MessageInfo Info
        {
            get => _info;
            set => _info = value ?? new MessageInfo();
        }

        /// <summary>
        /// Gets the collection of recipients and sender addresses of the message.
        /// If the collection contains an address of type <see cref="MailAddressType.TestAddress"/>, then
        /// all recipient addresses of the collection will be replaced with the mailbox address of the test address.
        /// </summary>
        [YAXSerializableField]
        [YAXCustomSerializer(typeof(MailMergeAddressesSerializer))]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public MailMergeAddressCollection MailMergeAddresses
        {
            get => _mailMergeAddresses;
            set
            {
                _mailMergeAddresses = value ?? new MailMergeAddressCollection(this);
                _mailMergeAddresses.MailMergeMessage = this;
            }
        }

        /// <summary>
        /// Gets or sets the user defined headers of a mail message.
        /// </summary>
        [YAXSerializableField]
        [YAXCustomSerializer(typeof(HeaderListSerializer))]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public HeaderList Headers
        {
            get => _headers;
            private set => _headers = value ?? new HeaderList();
        }

        #endregion

        #region *** Content properties ***

        /// <summary>
        /// Gets or sets the mail message subject.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string Subject { get => _subject; set => _subject = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets the mail message plain text content.
        /// </summary>
        [YAXSerializableField]
        [YAXCustomSerializer(typeof(StringAsCdataSerializer))]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string PlainText { get => _plainText; set => _plainText = value ?? string.Empty; }

        /// <summary>
        /// Gets or sets the mail message HTML content.
        /// </summary>
        [YAXSerializableField]
        [YAXCustomSerializer(typeof(StringAsCdataSerializer))]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string HtmlText { get => _htmlText; set => _htmlText = value ?? string.Empty; }

        /// <summary>
        /// Gets a collection of type <see cref="MailMergeLib.Templates.Templates"/>.
        /// Templates can be part of <see cref="PlainText"/> or <see cref="HtmlText"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public MailMergeLib.Templates.Templates Templates
        {
            get => _templates;
            private set => _templates = value ?? new Templates.Templates();
        }

        /// <summary>
        /// Gets or sets the instance of the MailSmartFormatter (derived from SmartFormat.NET's SmartFormatter) which will be used with MailMergeLib.
        /// </summary>
        [YAXDontSerialize]
        public MailSmartFormatter SmartFormatter { get; private set; }

        /// <summary>
        /// The settings for a MailMergeMessage.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public MessageConfig Config
        {
            get => _config;
            set
            {
                _config = value ?? new MessageConfig();
                if (_config.SmartFormatterConfig == null) _config.SmartFormatterConfig = new SmartFormatterConfig();

                SmartFormatter.SetConfig(_config.SmartFormatterConfig);
                _config.SmartFormatterConfig.OnConfigChanged += SmartFormatter.SetConfig;
            }
        }

        #endregion

        #region *** Attachment properties ***

        /// <summary>
        /// Gets or sets files that will be attached to a mail message.
        /// File names may contain placeholders.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public HashSet<FileAttachment> FileAttachments
        {
            get => _fileAttachments;
            private set => _fileAttachments = value ?? new HashSet<FileAttachment>();
        }

        /// <summary>
        /// Gets or sets streams that will be attached to a mail message.
        /// </summary>
        [YAXDontSerialize]
        public List<StreamAttachment> StreamAttachments
        {
            get => _streamAttachments;
            internal set => _streamAttachments = value ?? new List<StreamAttachment>();
        }

        /// <summary>
        /// Gets inline attachments (linked resources of the HTML body) of a mail message.
        /// They are generated automatically with all image sources pointing to local files.
        /// For adding non-automatic inline attachments, use <see cref="AddExternalInlineAttachment"/> ONLY.
        /// </summary>
        [YAXDontSerialize]
        public HashSet<FileAttachment> InlineAttachments
        {
            get => _inlineAttachments;
            private set => _inlineAttachments = value ?? new HashSet<FileAttachment>();
        }

        /// <summary>
        /// Gets or sets string attachments that will be attached to a mail message.
        /// String attachments can be text or binary.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public HashSet<StringAttachment> StringAttachments
        {
            get => _stringAttachments;
            private set => _stringAttachments = value ?? new HashSet<StringAttachment>();
        }

        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        private HashSet<FileAttachment> ExternalInlineAttachments
        {
            get => _externalInlineAttachments;
            set => _externalInlineAttachments = value ?? new HashSet<FileAttachment>();
        }

        /// <summary>
        /// Adds external inline attachments (linked resources of the HTML body) of a mail message.
        /// They are normally generated automatically with all image sources pointing to local files,
        /// but with this method such files can be added as well.
        /// </summary>
        /// <param name="att"></param>
        public void AddExternalInlineAttachment(FileAttachment att)
        {
            ExternalInlineAttachments.Add(att);
        }

        /// <summary>
        /// Clears external inline attachments (linked resources of the HTML body) of a mail message.
        /// They are normally generated automatically with all image sources pointing to local files.
        /// This method only removes attachments formerly added with AddExternalInlineAttachment.
        /// </summary>
        public void ClearExternalInlineAttachment()
        {
            ExternalInlineAttachments.Clear();
        }

        #endregion

        #region *** SmartFormat ***
        private MailSmartFormatter GetConfiguredMailSmartFormatter()
        {
            if (Config.SmartFormatterConfig == null) Config.SmartFormatterConfig = new SmartFormatterConfig();

            var smartFormatter = new MailSmartFormatter(Config.SmartFormatterConfig);
            // Smart.Format("{Name:choose(null|):N/A|empty|{Name}}", variable), where abc.Name NULL, string.Emtpy or a string

            smartFormatter.OnFormattingFailure += (sender, args) => { _badVariableNames.Add(args.Placeholder); };
            smartFormatter.Parser.OnParsingFailure += (sender, args) => { _parseExceptions.Add(new ParseException(args.Errors.MessageShort, args.Errors)); };
            return smartFormatter;
        }

        /// <summary>
        /// Replaces all variables in the text with their corresponding values.
        /// Used for subject, body and attachment.
        /// </summary>
        /// <param name="text">Text to search and replace.</param>
        /// <param name="dataItem"></param>
        /// <returns>Returns the text with all variables replaced.</returns>
        /// <remarks>
        /// In case <see cref="SmartFormat.Core.Settings.SmartSettings.FormatErrorAction"/> == ErrorAction.ThrowError
        /// or <see cref="SmartFormat.Core.Settings.SmartSettings.ParseErrorAction"/> == ErrorAction.ThrowError
        /// we simple catch the exception and simulate setting ErrorAction.MaintainTokens.
        /// Note: We track such errors by subscribing to Parser.OnParsingFailure and Formatter.OnFormattingFailure.
        /// </remarks>
        internal string SearchAndReplaceVars(string text, object dataItem)
        {
            if (text == null) return null;
            SmartFormatter.SetConfig(Config?.SmartFormatterConfig); // make sure we use the latest settings
            try
            {
                return SmartFormatter.Format(Config?.CultureInfo, text, dataItem);
            }
            catch (SmartFormat.Core.Parsing.ParsingErrors)
            {
                return text;
            }
            catch (SmartFormat.Core.Formatting.FormattingException)
            {
                return text;
            }
        }

        /// <summary>
        /// Replaces all variables in the text with their corresponding values.
        /// Filenames may contain backslashes which may not be interpreted as literals.
        /// That's why "ConvertCharacterStringLiterals" must be false in this method.
        /// Uses new instances of <see cref="MailSmartFormatter"/> and <see cref="SmartFormatterConfig"/>.
        /// </summary>
        /// <param name="text">Text to search and replace.</param>
        /// <param name="dataItem"></param>
        /// <returns>Returns the text with all variables replaced.</returns>
        /// <remarks>
        /// In case <see cref="SmartFormat.Core.Settings.SmartSettings.FormatErrorAction"/> == ErrorAction.ThrowError
        /// or <see cref="SmartFormat.Core.Settings.SmartSettings.ParseErrorAction"/> == ErrorAction.ThrowError
        /// we simple catch the exception and simulate setting ErrorAction.MaintainTokens.
        /// Note: We track such errors by subscribing to Parser.OnParsingFailure and Formatter.OnFormattingFailure.
        /// </remarks>
        internal string SearchAndReplaceVarsInFilename(string text, object dataItem)
        {
            if (text == null) return null;
            try
            {
                var filenameSmartFormatter = GetConfiguredMailSmartFormatter();
                filenameSmartFormatter.Settings.ConvertCharacterStringLiterals = false;
                return filenameSmartFormatter.Format(Config?.CultureInfo, text, dataItem);
            }
            catch (SmartFormat.Core.Parsing.ParsingErrors)
            {
                return text;
            }
            catch (SmartFormat.Core.Formatting.FormattingException)
            {
                return text;
            }
        }

        #endregion

        #region *** Private Methods ***

        /// <summary>
        /// Prepares the mail message subject:
        /// Replacing placeholders with their values and setting correct encoding.
        /// </summary>
        private void AddSubjectToMailMessage(MimeMessage msg, object dataItem)
        {
            var subject = SearchAndReplaceVars(Subject, dataItem);
            msg.Subject = subject ?? string.Empty;
            msg.Headers.Replace(HeaderId.Subject, Config.CharacterEncoding, msg.Subject);
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
                RegisterPlainTextTemplates();
                var plainText = SearchAndReplaceVars(PlainText, dataIteam);

                plainTextPart = (TextPart)new PlainBodyBuilder(plainText)
                {
                    TextTransferEncoding = Config.TextTransferEncoding,
                    CharacterEncoding = Config.CharacterEncoding
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
                RegisterHtmlTemplates();

                // create the HTML text body part with any linked resources
                // replacing any placeholders in the text or files with variable values
                var htmlBody = new HtmlBodyBuilder(this, dataIteam)
                {
                    DocBaseUri = Config.FileBaseDirectory,
                    TextTransferEncoding = Config.TextTransferEncoding,
                    BinaryTransferEncoding = Config.BinaryTransferEncoding,
                    CharacterEncoding = Config.CharacterEncoding
                };

                ExternalInlineAttachments.ToList().ForEach(ia => htmlBody.InlineAtt.Add(ia));

                if (alternative != null)
                {
                    alternative.Add(htmlBody.GetBodyPart());
                    _textMessagePart = alternative;
                }
                else
                {
                    _textMessagePart = htmlBody.GetBodyPart();
                }

                // get inline attachments and bad inline files AFTER htmlBody.GetBodyPart()!
                InlineAttachments = htmlBody.InlineAtt; // expose all resolved inline attachments in MailMergeMessage
                htmlBody.BadInlineFiles.ToList().ForEach(f => _badInlineFiles.Add(f));

                SmartFormatter.Templates.Clear();
            }
            else
            {
                InlineAttachments.Clear();
                _badInlineFiles.Clear();
            }
        }

        /// <summary>
        /// Registers html parts if they exist, otherwise the plain text parts
        /// </summary>
        private void RegisterHtmlTemplates()
        {
            if (SmartFormatter.Templates == null)
                SmartFormatter.Templates = new TemplateFormatter(SmartFormatter);

            SmartFormatter.Templates.Clear();

            foreach (var template in this.Templates)
            {
                var parts = template.GetParts();
                var htmlPart = parts.FirstOrDefault(p => p.Type == PartType.Html);
                if (htmlPart != null)
                {
                    SmartFormatter.Templates.Register(template.Name, htmlPart.Value ?? string.Empty);
                }
                else
                {
                    var plainPart = parts.FirstOrDefault(p => p.Type == PartType.Plain);
                    SmartFormatter.Templates.Register(template.Name, plainPart?.Value ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Registers only plain text parts
        /// </summary>
        private void RegisterPlainTextTemplates()
        {
            SmartFormatter.Templates.Clear();

            foreach (var template in this.Templates)
            {
                var plainPart = template.GetParts().FirstOrDefault(p => p.Type == PartType.Plain);
                SmartFormatter.Templates.Register(template.Name, plainPart?.Value ?? string.Empty);
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
                var filename = MakeFullPath(SearchAndReplaceVarsInFilename(fa.Filename, dataItem));
                var displayName = SearchAndReplaceVars(fa.DisplayName, dataItem);

                try
                {
                    _attachmentParts.Add(
                        new AttachmentBuilder(new FileAttachment(filename, displayName, fa.MimeType), Config.CharacterEncoding,
                            Config.TextTransferEncoding, Config.BinaryTransferEncoding).GetAttachment());
                }
                catch (FileNotFoundException)
                {
                    _badAttachmentFiles.Add(fa.Filename);
                }
                catch (IOException)
                {
                    _badAttachmentFiles.Add(fa.Filename);
                }
            }

            /* Note: all inline attachments (read from html body text or externally added ones
             * are processed in HtmlBodyBuilder
             */

            foreach (var sa in StreamAttachments)
            {
                var displayName = SearchAndReplaceVars(sa.DisplayName, dataItem);
                _attachmentParts.Add(
                    new AttachmentBuilder(new StreamAttachment(sa.Stream, displayName, sa.MimeType), Config.CharacterEncoding,
                        Config.TextTransferEncoding, Config.BinaryTransferEncoding).GetAttachment());
            }

            foreach (var sa in StringAttachments)
            {
                var displayName = SearchAndReplaceVars(sa.DisplayName, dataItem);
                _attachmentParts.Add(
                    new AttachmentBuilder(new StringAttachment(sa.Content, displayName, sa.MimeType), Config.CharacterEncoding,
                        Config.TextTransferEncoding, Config.BinaryTransferEncoding).GetAttachment());
            }
        }


        /// <summary>
        /// Calculates the full path of the file name, using the base directory if set.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>The full path of the file.</returns>
        private string MakeFullPath(string filename)
        {
            return Tools.MakeFullPath(Config.FileBaseDirectory, filename);
        }

        #endregion

        #region *** Public methods ***

        /// <summary>
        /// Converts the <see cref="HtmlText"/> into plain text (without tags or html entities)
        /// and writes it to the <see cref="PlainText"/> property.
        /// </summary>
        /// <param name="converter">
        /// The IHtmlConverter to be used for converting. If the converter is null, the 
        /// <see cref="AngleSharpHtmlConverter"/> will be used.
        /// </param>
        public void ConvertHtmlToPlainText(IHtmlConverter converter = null)
        {
            PlainText = converter == null
                ? new AngleSharpHtmlConverter().ToPlainText(HtmlText)
                : converter.ToPlainText(HtmlText);
        }

        /// <summary>
        /// Gets a list of MimeMessage representations of the MailMergeMessage for all items of the <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns>Returns all MailMessages ready to be sent by an SmtpClient.</returns>
        /// <exception cref="MailMergeMessageException">Throws a general <see cref="MailMergeMessageException"/>, which contains a list of exceptions giving more details.</exception>
        public IEnumerable<MimeMessage> GetMimeMessages<T>(IEnumerable<T> dataSource)
        {
            var dataSourceList = dataSource.ToList();
            foreach (var dataItem in dataSourceList)
            {
                yield return GetMimeMessage(dataItem);
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
        /// <exception cref="MailMergeMessageException">Throws a general <see cref="MailMergeMessageException"/>, which contains a list of exceptions giving more details.</exception>
        public MimeMessage GetMimeMessage(object dataItem = default)
        {
            lock (SyncRoot)
            {
                _badVariableNames.Clear();
                _parseExceptions.Clear();

                // convert DataRow to Dictionary<string, object>
                if (dataItem is DataRow row)
                {
                    dataItem = row.Table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c]);
                }

                var mimeMessage = new MimeMessage();
                AddSubjectToMailMessage(mimeMessage, dataItem);
                AddAddressesToMailMessage(mimeMessage, dataItem);
                AddAttributesToMailMessage(mimeMessage, dataItem);

                var exceptions = new List<Exception>();

                if (Config.FileBaseDirectory != string.Empty && !Tools.IsFullPath(Config.FileBaseDirectory))
                {
                    exceptions.Add(new DirectoryNotFoundException(
                        $"'{nameof(Config)}.{nameof(Config.FileBaseDirectory)}' is not a full path."));
                }

                try
                {
                    BuildTextMessagePart(dataItem);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                try
                {
                    BuildAttachmentPartsForMessage(dataItem);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                if (mimeMessage.To.Count == 0 && mimeMessage.Cc.Count == 0 && mimeMessage.Bcc.Count == 0)
                    exceptions.Add(new AddressException("No recipients.", _badMailAddr, null));
                if (string.IsNullOrWhiteSpace(mimeMessage.From.ToString()))
                    exceptions.Add(new AddressException("No from address.", _badMailAddr, null));
                if (string.IsNullOrEmpty(HtmlText) && string.IsNullOrEmpty(PlainText) && string.IsNullOrEmpty(Subject) && !FileAttachments.Any() &&
                    !InlineAttachments.Any() && !StringAttachments.Any() && !StreamAttachments.Any())
                    exceptions.Add(new EmtpyContentException("Message is empty.", null));
                if (_badMailAddr.Count > 0)
                    exceptions.Add(
                        new AddressException($"Bad mail address(es): {string.Join(", ", _badMailAddr.ToArray())}",
                            _badMailAddr, null));
                if (_badInlineFiles.Count > 0 && !Config.IgnoreMissingInlineAttachments)
                    exceptions.Add(
                        new AttachmentException(
                            $"Inline attachment(s) missing or not readable: {string.Join(", ", _badInlineFiles.ToArray())}",
                            _badInlineFiles, null));
                if (_badAttachmentFiles.Count > 0 && !Config.IgnoreMissingFileAttachments)
                    exceptions.Add(
                        new AttachmentException(
                            $"File attachment(s) missing or not readable: {string.Join(", ", _badAttachmentFiles.ToArray())}",
                            _badAttachmentFiles, null));
                if (_parseExceptions.Count > 0)
                    exceptions.AddRange(_parseExceptions);
                if (_badVariableNames.Count > 0)
                    exceptions.Add(
                        new VariableException(
                            $"Variable(s) for placeholder(s) not found: {string.Join(", ", _badVariableNames.ToArray())}",
                            _badVariableNames, null));

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
                    mimeMessage.Body = _textMessagePart ?? new TextPart("plain") { Text = string.Empty };
                }

                // Throw a general exception in case of any exceptions
                // Note: The MimeMessage, as far as it could completed, is one of the parameters of the exception
                if (exceptions.Count > 0)
                    throw new MailMergeMessageException("Building of message failed with one or more exceptions. See inner exceptions for details.", exceptions, mimeMessage);

                return mimeMessage;
            }
        }

        #endregion

        #region *** Destructor and IDisposable Members ***

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
                    StreamAttachments = null;
                }
            }
            _disposed = true;
        }

        #endregion

        #region *** Private methods ***

        /// <summary>
        /// Prepares all recipient address and the corresponding header fields of a mail message.
        /// </summary>
        private void AddAddressesToMailMessage(MimeMessage mimeMessage, object dataItem)
        {
            _badMailAddr.Clear();

            MailMergeAddress testAddress = null;
            foreach (MailMergeAddress mmAddr in MailMergeAddresses.Where(mmAddr => mmAddr.AddrType == MailAddressType.TestAddress))
            {
                testAddress = new MailMergeAddress(MailAddressType.TestAddress, mmAddr.Address, mmAddr.DisplayName);
            }

            if (Config.StandardFromAddress != null)
            {
                Config.StandardFromAddress.Address = SearchAndReplaceVars(Config.StandardFromAddress.Address, dataItem);
                Config.StandardFromAddress.Name = SearchAndReplaceVars(Config.StandardFromAddress.Name, dataItem);
                mimeMessage.From.Add(Config.StandardFromAddress);
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
                        mailboxAddr = testAddress.GetMailAddress(this, dataItem);
                    }
                    else
                    {
                        mailboxAddr = mmAddr.GetMailAddress(this, dataItem);
                    }

                    if (Config.IgnoreIllegalRecipientAddresses && mailboxAddr == null)
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
                            mimeMessage.Headers.RemoveAll(HeaderId.DispositionNotificationTo);
                            mimeMessage.Headers.Add(HeaderId.DispositionNotificationTo, mailboxAddr.Address);
                            break;
                        case MailAddressType.ReturnReceiptTo:
                            mimeMessage.Headers.RemoveAll(HeaderId.ReturnReceiptTo);
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

        /// <summary>
        /// Sets all attributes of a mail message.
        /// </summary>
        private void AddAttributesToMailMessage(MimeMessage mimeMessage, object dataItem)
        {
            mimeMessage.Priority = Config.Priority;

            if (!string.IsNullOrEmpty(Config.Xmailer))
            {
                mimeMessage.Headers.Replace(HeaderId.XMailer, Config.CharacterEncoding, Config.Xmailer);
            }

            if (!string.IsNullOrEmpty(Config.Organization))
            {
                mimeMessage.Headers.Replace(HeaderId.Organization, Config.CharacterEncoding, Config.Organization);
            }

            // collect any headers already present, e.g. headers for mailbox addresses like return-receipt
            var hl = new HashSet<string>();
            foreach (var header in mimeMessage.Headers)
            {
                hl.Add(header.Field);
            }
            // now add unique general headers from MailMergeMessage
            foreach (var header in Headers)
            {
                if (hl.Add(header.Field))
                    mimeMessage.Headers.Add(header.Field, Config.CharacterEncoding, SearchAndReplaceVars(header.Value, dataItem));
            }
        }

        #endregion

        #region *** Serialization ***

        /// <summary>
        /// Get the message as a serialized XML string.
        /// </summary>
        /// <returns>Returns a string with XML markup.</returns>
        public string Serialize()
        {
            return SerializationFactory.Serialize(this);
        }

        /// <summary>
        /// Write a message to an XML stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public void Serialize(Stream stream, Encoding encoding)
        {
            SerializationFactory.Serialize(this, stream, encoding);
        }

        /// <summary>
        /// Write message to an XML file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public void Serialize(string filename, Encoding encoding)
        {
            SerializationFactory.Serialize(this, filename, encoding);
        }

        /// <summary>
        /// Reads a message from an xml string.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static MailMergeMessage Deserialize(string xml)
        {
            return SerializationFactory.Deserialize<MailMergeMessage>(xml);
        }

        /// <summary>
        /// Reads a message from an xml stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public static MailMergeMessage Deserialize(Stream stream, Encoding encoding)
        {
#pragma warning disable IDE0068 // Use recommended dispose pattern
            return Deserialize(new StreamReader(stream, encoding), true);
#pragma warning restore IDE0068 // Use recommended dispose pattern
        }

        /// <summary>
        /// Reads message from an xml file.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding"></param>
        public static MailMergeMessage Deserialize(string filename, Encoding encoding)
        {
            return SerializationFactory.Deserialize<MailMergeMessage>(filename, encoding);
        }

        /// <summary>
        /// Reads a message xml with a StreamReader.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>Returns a <see cref="MailMergeMessage"/> instance.</returns>
        /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
        private static MailMergeMessage Deserialize(StreamReader reader, bool isStream)
        {
            return SerializationFactory.Deserialize<MailMergeMessage>(reader, isStream);
        }

        #endregion

        #region *** Equality ***

        /// <summary>
        /// Compares the MailMergeMessage with an other instance of MailMergeMessage for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns true, if both instances are equal, else false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MailMergeMessage)obj);
        }

        /// <summary>
        /// Compares the MailMergeMessage with an other instance of MailMergeMessage for equality.
        /// </summary>
        /// <param name="mmm"></param>
        /// <returns>Returns true, if both instances are equal, else false.</returns>
        /// <remarks>
        /// InlineAttachments are not compared because this property will be populated automatically from any HtmlText.
        /// StreamAttachments are not included in the comparison.
        /// </remarks>
        public bool Equals(MailMergeMessage mmm)
        {
            return Info.Equals(mmm.Info) &&
                   MailMergeAddresses.Equals(mmm.MailMergeAddresses) &&
                   Equals(FileAttachments, mmm.FileAttachments) &&
                   Equals(ExternalInlineAttachments, mmm.ExternalInlineAttachments) &&
                   Equals(StringAttachments, mmm.StringAttachments) &&
                   string.Equals(Subject, mmm.Subject) &&
                   string.Equals(PlainText, mmm.PlainText) &&
                   string.Equals(HtmlText, mmm.HtmlText) &&
                   Equals(Headers, mmm.Headers) &&
                   Config.Equals(mmm.Config);
        }

        private bool Equals(HeaderList hl1, HeaderList hl2)
        {
            var hl1Dict = hl1.ToDictionary(header => header.Id, header => header.Value);
            var h21Dict = hl2.ToDictionary(header => header.Id, header => header.Value);

            // not any entry missing in hl1Dict, nor in the other list
            return !hl1Dict.Except(h21Dict).Union(h21Dict.Except(hl1Dict)).Any();
        }

        private bool Equals(HashSet<FileAttachment> fl1, HashSet<FileAttachment> fl2)
        {
            // not any entry missing in fl1, nor in the other list
            return !fl1.Except(fl2).Union(fl2.Except(fl1)).Any();
        }

        private bool Equals(HashSet<StringAttachment> sa1, HashSet<StringAttachment> sa2)
        {
            // not any entry missing in sa11, nor in the other list
            return !sa1.Except(sa2).Union(sa2.Except(sa1)).Any();
        }

        #endregion

        #region *** Public helper methods ***

        /// <summary>
        /// Dispose the streams of file attachments and HTML inline file attachments,
        /// so that files are fully accessible again
        /// </summary>
        /// <param name="mimeMessage"></param>
        public static void DisposeFileStreams(MimeMessage mimeMessage)
        {
            if (mimeMessage == null) return;

            // Dispose the streams of file attachments
            foreach (var mimePart in mimeMessage.Attachments?.Where(mp => mp is MimePart)?.Cast<MimePart>())
            {
                mimePart?.Content?.Stream?.Dispose();
            }

            // Dispose the streams of HTML inline file attachments
            foreach (var mimePart in mimeMessage.BodyParts?.Where(mp => mp is MimePart)?.Cast<MimePart>())
            {
                mimePart?.Content?.Stream?.Dispose();
            }
        }

        #endregion
    }
}