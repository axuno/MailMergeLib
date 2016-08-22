using System;
using System.Text;
using MimeKit;

namespace MailMergeLib
{
	/// <summary>
	/// Container for a mail merge address.
	/// </summary>
	public class MailMergeAddress
	{
		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="address">A string that contains an e-mail address. Can include display name and address in one string, e.g. "recipient" &lt;recipient@mail.com&gt;.</param>
		public MailMergeAddress(MailAddressType addrType, string address)
			: this(addrType, string.Empty, address)
		{
		}

		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="address">A string that contains an e-mail address.</param>
		/// <param name="displayName">A string that contains the display name associated with address. This parameter can be null.</param>
		public MailMergeAddress(MailAddressType addrType, string displayName, string address)
		{
			AddrType = addrType;
			Address = address;
			DisplayName = displayName;
		}

		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="fullMailAddress">A string that contains a full e-mail address, which must include an address part, and may include a display name part, e.g. "Reci Name" &lt;name@example.com&gt;</param>
		/// <param name="displayNameCharacterEncoding">Encoding that defines the character set used for displayName.</param>
		public MailMergeAddress(MailAddressType addrType, string fullMailAddress, Encoding displayNameCharacterEncoding)
		{
			AddrType = addrType;
			DisplayNameCharacterEncoding = displayNameCharacterEncoding;
			MailboxAddress mba;
			if (MailboxAddress.TryParse(displayNameCharacterEncoding.GetBytes(fullMailAddress), out mba))
			{
				Address = mba.Address;
				DisplayName = mba.Name;
			}
			else
			{
				Address = fullMailAddress;
			}
		}

		/// <summary>
		/// Gets or sets the type of the MailMergeAddress.
		/// </summary>
		public MailAddressType AddrType { get; set; }

		/// <summary>
		/// Gets or sets the mail address of the recipient, e.g. "test@example.com"
		/// </summary>
		public string Address { get; set; }

		/// <summary>
		/// Gets or sets the display name of the recipient.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Gets or sets the Encoding that defines the character set used for displayName.
		/// </summary>
		internal Encoding DisplayNameCharacterEncoding { get; set; }
		
		/// <summary>
		/// Gets the MailAddress representation of the MailMergeAddress.
		/// </summary>
		/// <returns>Returns a MailAddress ready to be used for a MailAddress, or Null if no address part exists.</returns>
		/// <exception cref="NullReferenceException">Throws a NullReferenceException if TextVariableManager is null.</exception>
		/// <exception cref="FormatException">Throws a FormatException if the computed MailAddress is not valid.</exception>
		internal MailboxAddress GetMailAddress(MailSmartFormatter formatter, object dataItem)
		{
			string address = formatter.Format(Address, dataItem);
			string displayName = formatter.Format(DisplayName, dataItem);
			if (string.IsNullOrEmpty(displayName)) displayName = null;

			// Exclude invalid address from further process
			if (!EmailValidator.Validate(address, false, true))
			{
				return null;
			}

			return  displayName != null
			            ? new MailboxAddress(DisplayNameCharacterEncoding, displayName, address)
			            : new MailboxAddress(DisplayNameCharacterEncoding, address, address);
		}
	}
}