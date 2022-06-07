namespace MailMergeLib;

/// <summary>
/// Uri schemes
/// </summary>
/// <remarks>
/// Supplied because .Net Core &lt;= 1.6 does not include the Uri.[Scheme] public fields which are suppliedin the Uri class of .NET 4.x
/// </remarks>
internal class UriScheme
{
    /// <summary>
    /// Specifies the characters that separate the communication protocol scheme from the address portion of the URI. This field is read-only.
    /// </summary>
    /// <value>"://"</value>
    public static readonly string SchemeDelimiter = "://";
        
    /// <summary>
    /// Specifies that the URI is a pointer to a file. This field is read-only.
    /// </summary>
    /// <value>"file"</value>
    public static readonly string File = "file";

    /// <summary>
    /// Specifies that the URI is accessed through the File Transfer Protocol (FTP). This field is read-only.
    /// </summary>
    /// <value>"ftp"</value>
    public static readonly string Ftp = "ftp";

    /// <summary>
    /// Specifies that the URI is accessed through the Gopher protocol. This field is read-only.
    /// </summary>
    /// <value>"gopher"</value>
    public static readonly string Gopher = "gopher";

    /// <summary>
    /// Specifies that the URI is accessed through the Hypertext Transfer Protocol (HTTP). This field is read-only.
    /// </summary>
    /// <value>"http"</value>
    public readonly string Http = "http";

    /// <summary>
    /// Specifies that the URI is accessed through the Secure Hypertext Transfer Protocol (HTTPS). This field is read-only.
    /// </summary>
    /// <value>"https"</value>
    public static readonly string Https = "https";

    /// <summary>
    /// Specifies that the URI is an e-mail address and is accessed through the Simple Mail Transport Protocol (SMTP). This field is read-only.
    /// </summary>
    /// <value>"mailto"</value>
    public static readonly string Mailto = "mailto";

    /// <summary>
    /// Specifies that the URI is accessed through the NetPipe scheme used by Windows Communication Foundation (WCF). This field is read-only.
    /// </summary>
    /// <value>"net.pipe"</value>
    public static readonly string NetPipe = "net.pipe";

    /// <summary>
    /// Specifies that the URI is accessed through the NetTcp scheme used by Windows Communication Foundation (WCF). This field is read-only.
    /// </summary>
    /// <value>"net.tcp"</value>
    public readonly string NetTcp = "net.tcp";

    /// <summary>
    /// Specifies that the URI is an Internet news group and is accessed through the Network News Transport Protocol (NNTP). This field is read-only.
    /// </summary>
    /// <value>"news"</value>
    public static readonly string News = "news";

    /// <summary>
    /// Specifies that the URI is an Internet news group and is accessed through the Network News Transport Protocol (NNTP). This field is read-only.
    /// </summary>
    /// <value>"nntp"</value>
    public static readonly string Nntp = "nntp";
}