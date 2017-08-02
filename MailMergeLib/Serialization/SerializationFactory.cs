using System;
using YAXLib;

namespace MailMergeLib.Serialization
{
    /// <summary>
    /// Factory class for generating serialization related objects.
    /// </summary>
    public class SerializationFactory
    {
        /// <summary>
        /// Default namespace used during xml serialization.
        /// </summary>
        public const string Namespace = "http://www.axuno.net/MailMergeLib/XmlSchema/5.1";

        /// <summary>
        /// The namespace prefix for the DefaultNamespace
        /// </summary>
        public const string NamespacePrefix = "mmlib";

        /// <summary>
        /// Create a pre-configures YAXSerializer.
        /// </summary>
        /// <param name="classType"></param>
        /// <returns>Returns a pre-configured YAXSerializer.</returns>
        public static YAXSerializer GetStandardSerializer (Type classType)
        {
            return new YAXSerializer(classType, YAXExceptionHandlingPolicies.ThrowErrorsOnly, YAXExceptionTypes.Error,
                YAXSerializationOptions.SerializeNullObjects) {MaxRecursion = 50};
        }
    }
}
