using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MailMergeLib;

/// <summary>
/// Enumeration of the available types of a MailMergeAddress.
/// </summary>
public enum MailAddressType
{
    /// <summary>
    /// TO mailbox address.
    /// </summary>
    To,
    /// <summary>
    /// CC carbon copy mailbox address.
    /// </summary>
    CC,
    /// <summary>
    /// BCC blind carbon copy mailbox address.
    /// </summary>
    Bcc,
    /// <summary>
    /// FROM mailbox address.
    /// </summary>
    From,
    /// <summary>
    /// SENDER mailbox address.
    /// </summary>
    Sender,
    /// <summary>
    /// REPLY-TO mailbox address.
    /// </summary>
    ReplyTo,
    /// <summary>
    /// Mailbox address where to send read confirmations.
    /// </summary>
    ConfirmReadingTo,
    /// <summary>
    /// Mailbox address where to send return receipts.
    /// </summary>
    ReturnReceiptTo,
    /// <summary>
    /// If this type is used, all address parts (but not the display names) will be replaced by this address (for test purposes)
    /// </summary>
    TestAddress
}


/// <summary>
/// Container for mail merge addresses of a mail merge message.
/// </summary>
public class MailMergeAddressCollection : Collection<MailMergeAddress>
{
    /// <summary>
    /// Property <c>MailMergeMessage</c> must be set after creating the instance!
    /// </summary>
    internal MailMergeAddressCollection() {}

    /// <summary>
    /// Constructor.
    /// </summary>
    internal MailMergeAddressCollection(MailMergeMessage msg)
    {
        MailMergeMessage = msg;
    }

    /// <summary>
    /// The MailMergeMessage the MailMergeAddressCollection belongs to
    /// </summary>
    internal MailMergeMessage MailMergeMessage { get; set; }

    /// <summary>
    /// Adds the address to the address collection.
    /// For MailAddressType.Sender, MailAddressType.ConfirmReadingTo and MailAddressType.ReturnReceiptTo 
    /// only the last-in address of this type will be included in the mail message.
    /// <c>DisplayNameCharacterEncoding</c> will be overridden by <c>MailMergeMessage.Config.CharacterEncoding</c>.
    /// </summary>
    /// <param name="address"></param>
    public new void Add(MailMergeAddress address)
    {
        if (address.DisplayNameCharacterEncoding == null)
            address.DisplayNameCharacterEncoding = MailMergeMessage.Config.CharacterEncoding;

        base.Add(address);
    }

    /// <summary>
    /// Adds the address to the address collection.
    /// For MailAddressType.Sender, MailAddressType.ConfirmReadingTo and MailAddressType.ReturnReceiptTo 
    /// only the last-in address of this type will be included in the mail message.
    /// <c>DisplayNameCharacterEncoding</c> of the address will not be changed.
    /// </summary>
    /// <param name="address"></param>
    internal void AddWithCurrentCharacterEncoding(MailMergeAddress address)
    {
        base.Add(address);
    }

    /// <summary>
    /// Gets all MailMergeAddresses of the specified address type.
    /// </summary>
    /// <param name="addrType"></param>
    /// <returns>Returns all MailMergeAddresses of the specified address type.</returns>
    public IEnumerable<MailMergeAddress> Get(MailAddressType addrType)
    {
        return Items.Where(mmAddr => mmAddr.AddrType == addrType);
    }

    /// <summary>
    /// Gets the string representation of the collection of mailbox addresses.
    /// </summary>
    /// <param name="addrType"></param>
    /// <param name="dataItem"></param>
    /// <returns>The string representation of the collection of mailbox addresses</returns>
    public string ToString(MailAddressType addrType, object dataItem)
    {
        return string.Join(", ", Get(addrType).Select(at => at.GetMailAddress(MailMergeMessage, dataItem).ToString()));
    }

    #region *** Equality ***

    /// <summary>
    /// Determines whether the specified <c>MailMergeAddressCollection</c> instances are equal, i.e. containing the same <c>MailMergeAddress</c>es never mind the sequence.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MailMergeAddressCollection)obj);
    }

    /// <summary>
    /// Determines whether the specified <c>MailMergeAddressCollection</c> instances are equal, i.e. containing the same <c>MailMergeAddress</c>es never mind the sequence.
    /// </summary>
    /// <param name="addressCollection"></param>
    /// <returns></returns>
    protected bool Equals(MailMergeAddressCollection addressCollection)
    {
        if (addressCollection == null) return false;
        // not any address missing in this, nor in the other collection
        return !this.Except(addressCollection).Union(addressCollection.Except(this)).Any();
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Aggregate(0, (current, item) => unchecked (current * 397) ^ (item != null ? item.GetHashCode() : 0));
    }

    #endregion
}