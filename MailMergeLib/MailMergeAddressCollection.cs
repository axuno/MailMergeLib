using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
		internal MailMergeAddressCollection(ref MailMergeMessage msg)
		{
			_mailMergeMessage = msg;
		}

		public new void Add(MailMergeAddress address)
		{
			address.TextVariableManager = _mailMergeMessage.GetTextVariableManager();
			base.Add(address);
		}

		public MailMergeAddressCollection Get(MailAddressType addrType)
		{
			var addrCol = new MailMergeAddressCollection(ref _mailMergeMessage);

			foreach (var mmAddr in Items.Where(mmAddr => mmAddr.AddrType == addrType))
			{
				addrCol.Add(mmAddr);
			}

			return addrCol;
		}

		public string ToString(MailAddressType addrType)
		{
			TextVariableManager txtMgr = _mailMergeMessage.GetTextVariableManager().Clone();
			txtMgr.ShowEmptyAs = txtMgr.ShowNullAs = string.Empty;
			var addr = new StringBuilder();

			foreach (MailMergeAddress mmAddr in Get(addrType))
			{
				// copy all bad file names and bad variable names from "main" TextVariableManager
				txtMgr.BadFiles.AddRange(_mailMergeMessage.GetTextVariableManager().BadFiles);
				txtMgr.BadVariables.AddRange(_mailMergeMessage.GetTextVariableManager().BadVariables);

				mmAddr.TextVariableManager = txtMgr;
				addr.Append(mmAddr.ToString());

				// fill the "main" TextVariableManager with bad file/variable names
				_mailMergeMessage.GetTextVariableManager().BadFiles.Clear();
				_mailMergeMessage.GetTextVariableManager().BadFiles.AddRange(txtMgr.BadFiles);

				_mailMergeMessage.GetTextVariableManager().BadVariables.Clear();
				_mailMergeMessage.GetTextVariableManager().BadVariables.AddRange(txtMgr.BadVariables);

				addr.Append(", ");
			}

			if (addr.Length >= 2)
				addr.Remove(addr.Length - 2, 2);

			return addr.ToString();
		}
	}
}