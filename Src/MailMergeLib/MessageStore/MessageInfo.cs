using MailMergeLib.Serialization;
using YAXLib;

namespace MailMergeLib.MessageStore
{
    /// <summary>
    /// Metadata about a <see cref="MailMergeMessage"/>.
    /// </summary>
    public class MessageInfo : IMessageInfo
    {
        /// <summary>
        /// The Id of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public long Id { get; set; }

        /// <summary>
        /// The category of the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string Category { get; set; }

        /// <summary>
        /// Description for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// Comments for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string Comments { get; set; }

        /// <summary>
        /// Data hint for the <see cref="MailMergeMessage"/>. A user-defined string without further relevance for <see cref="MailMergeLib"/>.
        /// </summary>
        [YAXSerializableField]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXCustomSerializer(typeof(StringAsCdataSerializer))]
        public string Data { get; set; }

        #region *** Equality ***

        /// <summary>
        /// Determines whether the specified <see cref="MessageInfoBase"/> is equal to the current.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>bool</returns>
        public bool Equals(IMessageInfo other)
        {
            if (other == null) return false;
            return Id == other.Id && Category == other.Category && Description == other.Description && Comments == other.Comments && Data == other.Data;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is IMessageInfo)) return false;
            return Equals((IMessageInfo)obj);
        }

        /// <summary>
        /// Returns the hash code for this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id.GetHashCode();
                hashCode = (hashCode * 397) ^ (Category != null ? Category.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Comments != null ? Comments.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Data != null ? Data.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}
