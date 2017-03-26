using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MimeKit;

namespace MailMergeLib
{
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
        // the MailMergeMessage the MailMergeAddressCollection belongs to
        private MailMergeMessage _mailMergeMessage;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal MailMergeAddressCollection(MailMergeMessage msg)
        {
            _mailMergeMessage = msg;
        }

        /// <summary>
        /// Adds the address to the address collection.
        /// For MailAddressType.Sender, MailAddressType.ConfirmReadingTo and MailAddressType.ReturnReceiptTo 
        /// only the last-in address of this type will be included in the mail message.
        /// </summary>
        /// <param name="address"></param>
        public new void Add(MailMergeAddress address)
        {
            address.DisplayNameCharacterEncoding = _mailMergeMessage.Config.CharacterEncoding;
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
            return string.Join(", ", Get(addrType).Select(at => at.GetMailAddress(_mailMergeMessage, dataItem).ToString()));
        }
    }
}