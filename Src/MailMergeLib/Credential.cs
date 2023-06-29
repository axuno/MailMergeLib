using System;
using System.Net;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace MailMergeLib;

/// <summary>
/// Class used for serialization of credentials created from user name, password and (optional) domain.
/// Credentials will be saved encrypted. Only GetCredential(...) returns the NetworkCredential decrypted.
/// </summary>
[YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
public class Credential : ICredentials
{
    #region *** Constructor ***

    /// <summary>
    /// Initializes a new instance of the Credential class.
    /// </summary>
    public Credential()
    {
        Username = Password = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the Credential class.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="domain"></param>
    public Credential(string username, string password, string? domain = null)
    {
        Username = username;
        Password = password;
        Domain = domain; 
    }

    #endregion
        
    #region *** Interface ICredentials ***

    /// <summary>
    /// Get the network credential.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="authType"></param>
    /// <returns>Returns an instance of NetworkCredential.</returns>
    public NetworkCredential GetCredential(Uri uri, string authType)
    {
        return string.IsNullOrEmpty(Domain)
            ? new NetworkCredential(Username, Password).GetCredential(uri, authType)
            : new NetworkCredential(Username, Password, Domain).GetCredential(uri, authType);
    }

    #endregion

    #region *** MailMergeLib properties ***

    /// <summary>
    /// User name as plain text.
    /// </summary>
    [YAXDontSerialize]
    public string Username { get; set; }

    [YAXAttributeForClass]
    [YAXSerializeAs("Username")]
    [YAXSerializableField]
    internal string UsernameEncrypted
    {
        get => Settings.CryptoEnabled ? Crypto.Encrypt(Username) : Username;
        set => Username = Settings.CryptoEnabled ? Crypto.Decrypt(value) : value;
    }

    /// <summary>
    /// Password as plain text.
    /// </summary>
    [YAXDontSerialize]
    public string Password { get; set; }

    [YAXAttributeForClass]
    [YAXSerializeAs("Password")]
    [YAXSerializableField]
    internal string PasswordEncrypted
    {
        get => Settings.CryptoEnabled ? Crypto.Encrypt(Password) : Password;
        set => Password = Settings.CryptoEnabled ? Crypto.Decrypt(value) : value;
    }

    /// <summary>
    /// The domain.
    /// </summary>
    [YAXAttributeForClass]
    [YAXSerializableField]
    public string? Domain { get; set; }
        
    #endregion
}