using System.Collections.Specialized;
using System.Linq;
using YAXLib;

namespace MailMergeLib.Templates
{
    /// <summary>
    /// The class is a container for a list of type <see cref="Part"/>.
    /// </summary>
    /// <code>See the code sample for <see cref="Templates"/></code>
    [YAXSerializeAs("Template")]
    public class Template
    {
        private string _key;
        private string _defaultKey;
        private Parts _text = new Parts();

        /// <summary>
        /// Creates an instance of a <see cref="Template"/> class.
        /// </summary>
        public Template()
        {
            _text.CollectionChanged += TextOnCollectionChanged;
        }

        /// <summary>
        /// Creates an instance of a <see cref="Template"/> class.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        /// <param name="parts">The <see cref="Parts"/> of the template.</param>
        /// <param name="defaultKey">The default key for the template, may be omitted.</param>
        public Template(string name, Parts parts, string defaultKey = null) : this()
        {
            Name = name;
            Text.AddRange(parts);
            DefaultKey = defaultKey;
        }

        protected void TextOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            // call the setter to verify the current value can still be set after changing the collection
            DefaultKey = DefaultKey;
        }

        /// <summary>
        /// Gets or sets the Name of the <see cref="Template"/>
        /// </summary>
        [YAXSerializableField]
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Error)]
        public string Name { get; set; }

        /// <summary>
        /// The property is a list of type <see cref="Parts"/>.
        /// </summary>
        /// <exception cref="TemplateException"></exception>
        [YAXSerializableField]
        public Parts Text
        {
            get => _text;
            internal set
            {
                _text = value;
                _text.CollectionChanged += TextOnCollectionChanged;
            }
        }

        /// <summary>
        /// Gets the parts for the key parameter.
        /// If there is no part for the key, and the <see cref="DefaultKey"/> is not Null,
        /// the parts for the default key are returned. If neither can be found, but there are only max.
        /// 2 entries with 1 key, this one is returned. If all fail the returned array will be empty.
        /// </summary>
        /// <param name="key">The key to get the parts for. If null or omitted, the <see cref="Key"/> property of the <see cref="Template"/> will be used.</param>
        /// <returns>If the key parameter is found, it returns an array of <see cref="Part"/> for the key parameter, else from the default key.</returns>
        public Part[] GetParts(string key = null)
        {
            if (key == null) key = Key;

            // Gracious detection:
            // If Text only has entries with 1 key and nothing else is selected, return the parts for the only key
            var onlyOneKeyInParts = Text.GroupBy(p => p.Key, (k, g) => new {Key = k}).ToList();
            if (key == null && DefaultKey == null && onlyOneKeyInParts.Count == 1)
            {
                return this[onlyOneKeyInParts.First().Key];
            }

            if (key == null && DefaultKey != null)
            {
                return this[DefaultKey];
            }

            return this[key];
        }

        /// <summary>
        /// Sets or gets the key to be used for mail merge of this <see cref="Template"/>.
        /// </summary>
        /// <exception cref="TemplateException"></exception>
        [YAXDontSerialize]
        public string Key
        {
            get => _key;
            set
            {
                if (value != null && this[value].Length == 0)
                    throw new TemplateException($"Illegal value for {nameof(DefaultKey)}: No entry in the parts list has a key value of '{value}'.", null, null, this, null);

                _key = value;
            }
        }

        /// <summary>
        /// Sets or gets the default key of this <see cref="Template"/>.
        /// The DefaultKey setting usually comes from deserialized XML.
        /// </summary>
        /// <exception cref="TemplateException"></exception>
        [YAXAttributeFor("Text")]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXDontSerializeIfNull]
        public string DefaultKey
        {
            get => _defaultKey;
            set
            {
                if (value != null && this[value].Length == 0)
                    throw new TemplateException($"Illegal value for {nameof(DefaultKey)}: No entry in the parts list has a key value of '{value}'.", null, null, this, null);

                _defaultKey = value;
            }
        }

        /// <summary>
        /// Gets an array of type <see cref="Part"/> for the specified index.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns an array with one or two parts, or an empty array if the key does not exist.</returns>
        public Part[] this[string key]
        {
            get
            {
                return Text.Where(c => c.Key == key).ToArray();
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns true, if both instances are equal, else false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Template)obj);
        }

        protected bool Equals(Template other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Key, other.Key) && string.Equals(DefaultKey, other.DefaultKey) && Equals(Text, other.Text);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DefaultKey != null ? DefaultKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Text != null ? Text.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}