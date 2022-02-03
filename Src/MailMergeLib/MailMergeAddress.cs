using System;
using System.Text;
using MimeKit;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace MailMergeLib
{
    /// <summary>
    /// Container for a mail merge address.
    /// </summary>
    [YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
    public class MailMergeAddress
    {
        /// <summary>
        /// Represents the address of a mail sender or recipient for use with a MailMergeMessage.
        /// </summary>
        public MailMergeAddress() { }

        /// <summary>
        /// Represents the address of a mail sender or recipient for use with a MailMergeMessage.
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
            DisplayNameCharacterEncoding = null;
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
            if (MailboxAddress.TryParse(displayNameCharacterEncoding?.GetBytes(fullMailAddress), out mba))
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
        [YAXSerializableField]
        public MailAddressType AddrType { get; set; }

        /// <summary>
        /// Gets or sets the mail address of the recipient, e.g. "test@example.com"
        /// </summary>
        [YAXSerializableField]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the display name of the recipient.
        /// </summary>
        [YAXSerializableField]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Encoding that defines the character set used for displayName.
        /// </summary>
        [YAXDontSerialize]
        internal Encoding DisplayNameCharacterEncoding { get; set; }

        /// <summary>
        /// Character encoding for the display name.
        /// Used for serialization. It is the string representation of <see cref="DisplayNameCharacterEncoding"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXSerializeAs("DisplayNameCharacterEncoding")]
        internal string DisplayNameCharacterEncodingName
        {
            get { return DisplayNameCharacterEncoding.WebName; }
            set { DisplayNameCharacterEncoding = Encoding.GetEncoding(value); }
        }

        /// <summary>
        /// Gets the MailAddress representation of the MailMergeAddress.
        /// </summary>
        /// <returns>Returns a MailAddress ready to be used for a MailAddress, or Null if no address part exists.</returns>
        /// <exception cref="NullReferenceException">Throws a NullReferenceException if TextVariableManager is null.</exception>
        /// <exception cref="FormatException">Throws a FormatException if the computed MailAddress is not valid.</exception>
        internal MailboxAddress GetMailAddress(MailMergeMessage mmm, object dataItem)
        {
            var address = mmm.SearchAndReplaceVars(Address, dataItem);
            var displayName = mmm.SearchAndReplaceVars(DisplayName, dataItem);
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

        #region *** Equality ***

        /// <summary>
        /// Determines whether the specified MailMergeAddress instances are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MailMergeAddress) obj);
        }

        protected bool Equals(MailMergeAddress other)
        {
            return AddrType == other.AddrType && string.Equals(Address, other.Address) && string.Equals(DisplayName, other.DisplayName) && Equals(DisplayNameCharacterEncoding, other.DisplayNameCharacterEncoding);
        }

        /// <summary>
        /// Returns the hash code for the MailMergeAddress
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) AddrType;
                hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayNameCharacterEncoding != null ? DisplayNameCharacterEncoding.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}