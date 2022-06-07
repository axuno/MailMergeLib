using System.Collections.Generic;

namespace MailMergeLib.MessageStore;

/// <summary>
/// Interface for a messsge store that is able to search for deserialized <see cref="MailMergeMessage"/>s
/// and to read their metadata (<see cref="IMessageInfo"/>).
/// </summary>
public interface IMessageStore
{
    /// <summary>
    /// Scans for deserialized <see cref="MailMergeMessage"/>s.
    /// </summary>
    /// <returns>Returns an enumeration of <see cref="IMessageInfo"/>s.</returns>
    IEnumerable<MessageInfoBase> ScanForMessages();
}