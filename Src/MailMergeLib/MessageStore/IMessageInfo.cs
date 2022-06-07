namespace MailMergeLib.MessageStore;

/// <summary>
/// Metadata about a <see cref="MailMergeMessage"/>.
/// </summary>
public interface IMessageInfo
{
    /// <summary>
    /// The Id of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
    /// </summary>
    long Id { get; set; }

    /// <summary>
    /// The category of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
    /// </summary>
    string Category { get; set; }

    /// <summary>
    /// Description for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Comments for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
    /// </summary>
    string Comments { get; set; }

    /// <summary>
    /// Data hint for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
    /// </summary>
    string Data { get; set; }

    bool Equals(IMessageInfo obj);
}