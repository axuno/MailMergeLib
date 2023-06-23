using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using MimeKit;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace MailMergeLib;

/// <summary>
/// Configuration for MailMergeMessage.
/// </summary>
[YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly, Options = YAXSerializationOptions.DontSerializeNullObjects)]
public class MessageConfig
{
    private string _fileBaseDirectory = Path.GetTempPath();

    /// <summary>
    /// CTOR for MailMergeMessage configuration.
    /// </summary>
    public MessageConfig()
    { }

    /// <summary>
    /// Content transfer encoding for text like HTML.
    /// </summary>
    [YAXSerializableField]
    public ContentEncoding TextTransferEncoding { get; set; } = ContentEncoding.SevenBit;
    /// <summary>
    /// Content transfer encoding for binary content like image attachments.
    /// </summary>
    [YAXSerializableField]
    public ContentEncoding BinaryTransferEncoding { get; set; } = ContentEncoding.Base64;

    /// <summary>
    /// Character encoding.
    /// Used for serialization. It is the string representation of <see cref="CharacterEncoding"/>.
    /// </summary>
    [YAXSerializableField]
    [YAXSerializeAs("CharacterEncoding")]
    private string CharacterEncodingName
    {
        get => CharacterEncoding.WebName;
        set => CharacterEncoding = Encoding.GetEncoding(value);
    }

    /// <summary>
    /// Character encoding.
    /// </summary>
    public Encoding CharacterEncoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Culture information.
    /// Used for serialization. It is the string representation of <see cref="CultureInfo"/>.
    /// </summary>
    [YAXSerializableField]
    [YAXSerializeAs("CultureInfo")]
    private string CultureInfoName
    {
        get => CultureInfo.Name;
        set => CultureInfo = new CultureInfo(value);
    }

    /// <summary>
    /// Culture information.
    /// </summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets the local base directory of HTML linked resources and other attachments.
    /// The <see cref="FileBaseDirectory"/> must be a an absolute path *when processing the message* (not when setting the value), while *file paths are relative* to the <see cref="FileBaseDirectory"/>.
    /// It is useful for retrieval of inline attachments (linked resources of the HTML body).
    /// Defaults to <see cref="string.Empty"/>.
    /// </summary>
    [YAXSerializableField]
    public string FileBaseDirectory
    {
        get => _fileBaseDirectory;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _fileBaseDirectory = string.Empty;
                return;
            }
            _fileBaseDirectory = value;
        }
    }

    /// <summary>
    /// If true, empty or illegal recipient addresses will be discarded.
    /// If false, an exception will be thrown.
    /// </summary>
    [YAXSerializableField]
    public bool IgnoreIllegalRecipientAddresses { get; set; } = true;

    /// <summary>
    /// If true, missing or not readable inline attachments will be discarded.
    /// If false, an exception will be thrown.
    /// </summary>
    [YAXSerializableField]
    public bool IgnoreMissingInlineAttachments { get; set; } = false;

    /// <summary>
    /// If true, missing or not readable file attachments will be discarded.
    /// If false, an exception will be thrown.
    /// </summary>
    [YAXSerializableField]
    public bool IgnoreMissingFileAttachments { get; set; } = false;

    /// <summary>
    /// The priority header for the mail message.
    /// </summary>
    [YAXSerializableField]
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// The standard mailbox address which will be used as one of the "from" addresses.
    /// Used for serialization. It is the string representation of <see cref="StandardFromAddress"/>. 
    /// </summary>
    [YAXSerializableField]
    [YAXSerializeAs("StandardFromAddress")]
    private string? StandardFromAddressText
    {
        get => StandardFromAddress?.ToString();
        set => StandardFromAddress = !string.IsNullOrEmpty(value) ? MailboxAddress.Parse(ParserOptions.Default, value) : null;
    }

    /// <summary>
    /// The standard mailbox address which will be used as one of the "from" addresses.
    /// </summary>
    public MailboxAddress? StandardFromAddress { get; set; }

    /// <summary>
    /// The organization header of a mail message.
    /// </summary>
    [YAXSerializableField]
    public string? Organization { get; set; }

    /// <summary>
    /// Gets or sets the "x-mailer" header value to be used.
    /// </summary>
    [YAXSerializableField]
    public string? Xmailer { get; set; }

    /// <summary>
    /// SmartFormatter configuration for parsing and formatting errors.
    /// </summary>
    [YAXSerializableField]
    public SmartFormatterConfig SmartFormatterConfig { get; internal set; } = new SmartFormatterConfig();

    #region *** Equality ***
    
    /// <summary>
    /// Compares for equality
    /// </summary>
    protected bool Equals(MessageConfig other)
    {
        return TextTransferEncoding == other.TextTransferEncoding &&
               BinaryTransferEncoding == other.BinaryTransferEncoding &&
               Equals(CharacterEncoding, other.CharacterEncoding) &&
               Equals(CultureInfo, other.CultureInfo) &&
               string.Equals(FileBaseDirectory, other.FileBaseDirectory) &&
               IgnoreIllegalRecipientAddresses == other.IgnoreIllegalRecipientAddresses &&
               IgnoreMissingInlineAttachments == other.IgnoreMissingInlineAttachments &&
               IgnoreMissingFileAttachments == other.IgnoreMissingFileAttachments &&
               Priority == other.Priority &&
               Equals(StandardFromAddress, other.StandardFromAddress) &&
               string.Equals(Organization, other.Organization) &&
               string.Equals(Xmailer, other.Xmailer) &&
               SmartFormatterConfig.Equals(other.SmartFormatterConfig);
    }

    private bool Equals(MailboxAddress? addr, MailboxAddress? otherAddr)
    {
        if (addr is null && otherAddr is null) return true;
        if (ReferenceEquals(addr, otherAddr)) return true;
        if (otherAddr?.GetType() != addr?.GetType()) return false;
        return addr?.ToString() == otherAddr?.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MessageConfig)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)TextTransferEncoding;
            hashCode = (hashCode * 397) ^ (int)BinaryTransferEncoding;
            hashCode = (hashCode * 397) ^ (CharacterEncoding != null ? CharacterEncoding.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (CultureInfo != null ? CultureInfo.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (FileBaseDirectory != null ? FileBaseDirectory.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ IgnoreIllegalRecipientAddresses.GetHashCode();
            hashCode = (hashCode * 397) ^ IgnoreMissingInlineAttachments.GetHashCode();
            hashCode = (hashCode * 397) ^ IgnoreMissingFileAttachments.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Priority;
            hashCode = (hashCode * 397) ^ (StandardFromAddress != null ? StandardFromAddress.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Organization != null ? Organization.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Xmailer != null ? Xmailer.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (SmartFormatterConfig != null ? SmartFormatterConfig.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}