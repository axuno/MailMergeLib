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
			: this(addrType, address, string.Empty)
		{
		}

		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="address">A string that contains an e-mail address.</param>
		/// <param name="displayName">A string that contains the display name associated with address. This parameter can be null.</param>
		public MailMergeAddress(MailAddressType addrType, string address, string displayName)
			: this(addrType, address, displayName, Encoding.Default)
		{
		}

		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="address">A string that contains an e-mail address.</param>
		/// <param name="displayName">A string that contains the display name associated with address. This parameter can be null.</param>
		/// <param name="displayNameCharacterEncoding">Encoding that defines the character set used for displayName.</param>
		public MailMergeAddress(MailAddressType addrType, string address, string displayName,
		                        Encoding displayNameCharacterEncoding)
		{
			AddrType = addrType;
			Address = address;
			DisplayName = displayName;
			DisplayNameCharacterEncoding = displayNameCharacterEncoding;
		}

		/// <summary>
		/// Represents the address of an electronic mail sender or recipient for use with a MailMergeMessage.
		/// </summary>
		/// <param name="addrType">MailAddressType of the e-mail address.</param>
		/// <param name="fullMailAddress">A string that contains a full e-mail address, which must include an address part, and may include a display name part, e.g. "Reci Name" &lt;name@example.com&gt;</param>
		/// <param name="displayNameCharacterEncoding">Encoding that defines the character set used for displayName.</param>
		public MailMergeAddress(MailAddressType addrType, string fullMailAddress, Encoding displayNameCharacterEncoding)
		{
			string displayName, address;
			ParseMailAddress(fullMailAddress, out displayName, out address);
			AddrType = addrType;
			Address = address;
			DisplayName = displayName;
			DisplayNameCharacterEncoding = displayNameCharacterEncoding;
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
		public Encoding DisplayNameCharacterEncoding { get; set; }

		/// <summary>
		/// Gets or sets the TextVariableManager for replacing placeholders
		/// by variable values.
		/// </summary>
		internal TextVariableManager TextVariableManager { get; set; }

		/// <summary>
		/// Gets the MailAddress representation of the MailMergeAddress.
		/// </summary>
		/// <returns>Returns a MailAddress ready to be used for a MailAddress, or Null if no address part exists.</returns>
		/// <exception cref="NullReferenceException">Throws a NullReferenceException if TextVariableManager is null.</exception>
		/// <exception cref="FormatException">Throws a FormatException if the computed MailAddress is not valid.</exception>
		internal MailboxAddress GetMailAddress()
		{
			string address = TextVariableManager.Process(Address, false);
			string displayName = TextVariableManager.Process(DisplayName, false);
			if (string.IsNullOrEmpty(displayName)) displayName = null;

			// let caller decide how to deal with empty or illegal address parts
			// which exist after replacing placeholders with values.
			try
			{
				new MailboxAddress(DisplayNameCharacterEncoding, displayName, address);
			}
			catch
			{
				return null;
			}

			var mailAddress = displayName != null
			                          	? new MailboxAddress(DisplayNameCharacterEncoding, displayName, address)
			                          	: new MailboxAddress(DisplayNameCharacterEncoding, address, address);
			return mailAddress;
		}

		/// <summary>
		/// Convert the MailMergeAddress to a string.
		/// </summary>
		/// <returns>Return the string representation of the MailMergeAddress, 
		/// with all placeholders replaced by variable values.</returns>
		public override string ToString()
		{
			var addr = GetMailAddress();
			return addr?.ToString() ?? string.Empty;
		}

		private static void ParseMailAddress(string inputAddr, out string displayName, out string address)
		{
			displayName = null;
			address = null;

			if (string.IsNullOrEmpty(inputAddr))
				return;

			inputAddr = inputAddr.Trim();
			// display name should start with quotation mark
			int pos = inputAddr.IndexOf('"');
			if (pos == 0)
			{
				// get ending quotation mark
				pos = inputAddr.IndexOf('"', 1);
				if (pos > 0 && inputAddr.Length != (pos + 1))
				{
					displayName = inputAddr.Substring(1, pos - 1);
					inputAddr = inputAddr.Substring(pos + 1);
				}
			}

			// display name was not quoted
			if (displayName == null)
			{
				pos = inputAddr.IndexOf('<');
				if (pos > 0)
				{
					displayName = inputAddr.Substring(0, pos - 1).Trim();
					inputAddr = inputAddr.Substring(pos + 1);
				}
			}

			if (displayName != null)
			{
				pos = inputAddr.IndexOf('<');
				if (pos > 0)
				{
					inputAddr = inputAddr.Substring(pos);
				}
			}

			// Note: We do not check for presence of "@" in the address, because
			// a MailMergeAddress can be formatted like "{DisplayName} <{Address}>"
			address = inputAddr.TrimStart(new[] {'<'}).TrimEnd(new[] {'>'}).Trim();
		}
	}
}