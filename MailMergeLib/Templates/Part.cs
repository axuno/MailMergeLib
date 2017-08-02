using MailMergeLib.Serialization;
using YAXLib;

namespace MailMergeLib.Templates
{
    /// <summary>
    /// The class represents the single list item of type <see cref="Part"/>.
    /// To change a <see cref="Part"/> remove the part to change and add a new one.
    /// </summary>
    /// <code>See the code sample for <see cref="Templates"/></code>
    [YAXSerializeAs("Part")]
    [YAXCustomSerializer(typeof(PartSerializer))]
    public class Part
    {
        private string _value;

        /// <summary>
        /// Initialize an instance of a part.
        /// </summary>
        public Part()
        {}

        /// <summary>
        /// Initialize an instance of a part.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public Part(PartType type, string key, string value) : this()
        {
            Type = type;
            Key = key;
            _value = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the key of a part.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the <see cref="PartType"/> of a part.
        /// </summary>

        public PartType Type { get; private set; } = PartType.Plain;

        /// <summary>
        /// Gets the value (i.e. the Text or Html) of the part.
        /// Null assignments will be changed to empty string.
        /// </summary>
        public string Value
        {
            get => _value;
            private set => _value = value ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns true, if both instances are equal, else false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Part)obj);
        }

        /// <summary>
        /// Compares the Part with an other instance of Part for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>Returns true, if both instances are equal, else false.</returns>
        protected bool Equals(Part other)
        {
            return Key == other.Key && Type == other.Type && Value == other.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
        }
    }
}