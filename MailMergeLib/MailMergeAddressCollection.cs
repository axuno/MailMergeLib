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
		To,
		CC,
		Bcc,
		From,
		Sender,
		ReplyTo,
		ConfirmReadingTo,
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

		public new void Add(MailMergeAddress address)
		{
			address.DisplayNameCharacterEncoding = _mailMergeMessage.Config.CharacterEncoding;
			base.Add(address);
		}

		public IEnumerable<MailMergeAddress> Get(MailAddressType addrType)
		{
			return Items.Where(mmAddr => mmAddr.AddrType == addrType);
		}

		public string ToString(MailAddressType addrType, object dataItem)
		{
			return string.Join(", ", Get(addrType).Select(at => at.GetMailAddress(_mailMergeMessage.SmartFormatter, dataItem).ToString()));
		}
	}
}