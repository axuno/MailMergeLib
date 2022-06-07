using System;
using System.IO;
using System.Text;
using YAXLib;
using YAXLib.Enums;
using YAXLib.Options;

namespace MailMergeLib.Serialization;

/// <summary>
/// Factory class for generating serialization related objects.
/// </summary>
internal class SerializationFactory
{
    /// <summary>
    /// Default namespace used during xml serialization.
    /// </summary>
    internal const string Namespace = "http://www.axuno.net/MailMergeLib/XmlSchema/5.3";

    /// <summary>
    /// The namespace prefix for the DefaultNamespace
    /// </summary>
    internal const string NamespacePrefix = "mmlib";

    /// <summary>
    /// Create a pre-configures YAXSerializer.
    /// </summary>
    /// <param name="classType"></param>
    /// <returns>Returns a pre-configured YAXSerializer.</returns>
    internal static YAXSerializer GetStandardSerializer (Type classType)
    {
        return new YAXSerializer(classType,
            new SerializerOptions
            {
                ExceptionHandlingPolicies = YAXExceptionHandlingPolicies.ThrowErrorsOnly,
                ExceptionBehavior = YAXExceptionTypes.Error,
                MaxRecursion = 50,
                SerializationOptions = YAXSerializationOptions.SerializeNullObjects
            });
    }
        
    /// <summary>
    /// Serialize type T to XML.
    /// </summary>
    /// <returns>Returns a string with XML markup.</returns>
    internal static string Serialize<T>(T obj)
    {
        var serializer = GetStandardSerializer(typeof(T));
        return serializer.Serialize(obj);
    }

    /// <summary>
    /// Write type T to an XML stream.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    internal static void Serialize<T>(T obj, Stream stream, System.Text.Encoding encoding)
    {
        Serialize(obj, new StreamWriter(stream, encoding), true);
    }

    /// <summary>
    /// Write type T to a file.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="filename"></param>
    /// <param name="encoding"></param>
    internal static void Serialize<T>(T obj, string filename, Encoding encoding)
    {
        using (var fs = new FileStream(filename, FileMode.Create))
        {
            using (var sr = new StreamWriter(fs, encoding))
            {
                Serialize<T>(obj, sr, false);
            }
        }
    }

    /// <summary>
    /// Write type T with a StreamWriter.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="writer"></param>
    /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
    internal static void Serialize<T>(T obj, TextWriter writer, bool isStream)
    {
        var serializer = GetStandardSerializer(typeof(T));
        serializer.Serialize(obj, writer);
        writer.Flush();

        if (isStream) return;

#if NETFRAMEWORK
        writer.Close();
#endif
        writer.Dispose();
    }

    /// <summary>
    /// Deserialize the parameter with XML markup to an instance of T.
    /// </summary>
    /// <param name="xml"></param>
    /// <returns>Returns an instance of T.</returns>
    internal static T Deserialize<T>(string xml)
    {
        var serializer = GetStandardSerializer(typeof(T));
        return (T)serializer.Deserialize(xml);
    }

    /// <summary>
    /// Reads T from an XML stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encoding"></param>
    internal static T Deserialize<T>(Stream stream, Encoding encoding)
    {
        return Deserialize<T>(new StreamReader(stream, encoding), true);
    }

    /// <summary>
    /// Reads T from an XML file.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="encoding"></param>
    internal static T Deserialize<T>(string filename, System.Text.Encoding encoding)
    {
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (var sr = new StreamReader(fs, encoding))
            {
                return Deserialize<T>(sr, false);
            }
        }
    }

    /// <summary>
    /// Reads T XML with a StreamReader.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns>Returns a T instance.</returns>
    /// <param name="isStream">If true, the writer will not be closed and disposed, so that the underlying stream can be used on return.</param>
    internal static T Deserialize<T>(StreamReader reader, bool isStream)
    {
        var serializer = GetStandardSerializer(typeof(T));
        reader.BaseStream.Position = 0;
        var str = reader.ReadToEnd();
        var s = (T)serializer.Deserialize(str);

        if (isStream) return s;
#if NETFRAMEWORK
        reader.Close();
#endif
        reader.Dispose();

        return s;
    }
}