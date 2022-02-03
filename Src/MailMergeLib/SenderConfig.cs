using System.Linq;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace MailMergeLib
{
    /// <summary>
    /// Configuration for MailMergeSender.
    /// </summary>
    [YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
    public class SenderConfig
    {
        private int _maxNumOfSmtpClients = 5;

        /// <summary>
        /// CTOR for MailMergeSender configuration.
        /// </summary>
        public SenderConfig()
        {}

        /// <summary>
        /// Gets or sets the maximum number of SmtpClient to send messages concurrently.
        /// Valid numbers are 1 to 50, defaults to 5.
        /// </summary>
        [YAXSerializableField]
        public int MaxNumOfSmtpClients
        {
            get { return _maxNumOfSmtpClients; }
            set
            {
                if (value <= 0) _maxNumOfSmtpClients = 1;
                else if (value > 50) _maxNumOfSmtpClients = 50;
                else _maxNumOfSmtpClients = value;
            }
        }

        /// <summary>
        /// Gets or sets the array of configurations the SmtpClients will use.
        /// The first SmtpClientConfig is the "standard", any second is the "backup".
        /// Other instances of SmtpClientConfig in the array are used for parallel sending messages.
        /// </summary>
        [YAXSerializableField]
        [YAXSerializeAs("SmtpClients")]
        public SmtpClientConfig[] SmtpClientConfig { get; set; } = {new SmtpClientConfig()};

        #region *** Equality ***

        protected bool Equals(SenderConfig other)
        {
            if (MaxNumOfSmtpClients != other.MaxNumOfSmtpClients || SmtpClientConfig.Length != other.SmtpClientConfig.Length)
                return false;

            return !SmtpClientConfig.Where((t, i) => !t.Equals(other.SmtpClientConfig[i])).Any();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SenderConfig) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (MaxNumOfSmtpClients * 397) ^ (SmtpClientConfig != null ? SmtpClientConfig.GetHashCode() : 0);
            }
        }

        #endregion
    }
}
