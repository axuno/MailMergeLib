using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MimeKit;

namespace MailMergeLib
{
    /// <summary>
    /// Tools used by classes of MailMergeLib.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Checks whether the given path is a full path, depending on the platform.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if the path is absolute, else false.</returns>
        public static bool IsFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !Path.IsPathRooted(path))
                return false;
#if NETSTANDARD
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }
#endif
            var pathRoot = Path.GetPathRoot(path);
            if (pathRoot.Length <= 2) // Accepts X:\ and \\UNC\PATH, rejects empty string, \ and X:
                return false;
            return !(pathRoot == path && pathRoot.StartsWith("\\\\") && pathRoot.IndexOf('\\', 2) == -1); // A UNC server name without a share name (e.g "\\NAME") is invalid
        }

        /// <summary>
        /// Combines the specified filename with the basename of 
        /// to form a full path to file or directory.
        /// </summary>
        /// <param name="basename">The relative or absolute path.</param>
        /// <param name="filename">The filename, which may include a relative path.</param>
        /// <returns>
        /// A rooted path.
        /// </returns>
        public static string MakeFullPath(string basename, string filename)
        {
            basename = basename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (! string.IsNullOrEmpty(filename))
            {
                if (! Path.IsPathRooted(filename))
                {
                    filename = Path.GetFullPath(Path.Combine(basename, filename));
                }
            }
            return filename;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another. Replaces Windows' PathRelativePathTo function.
        /// </summary>
        /// <param name="fromDirectory">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string RelativePathTo(string fromDirectory, string toPath)
        {
            if (fromDirectory == null)
                throw new ArgumentNullException(nameof(fromDirectory));
            if (toPath == null)
                throw new ArgumentNullException(nameof(toPath));
            if (Path.IsPathRooted(fromDirectory) && Path.IsPathRooted(toPath))
            {
                if (string.Compare(Path.GetPathRoot(fromDirectory),
                                   Path.GetPathRoot(toPath), StringComparison.OrdinalIgnoreCase) != 0)
                {
                    throw new ArgumentException(
                        $"The paths '{fromDirectory} and '{toPath}' have different path roots.");
                }
            }
            var relativePath = new StringCollection();
            var fromDirectories = fromDirectory.Split(Path.DirectorySeparatorChar);
            var toDirectories = toPath.Split(Path.DirectorySeparatorChar);
            var length = Math.Min(fromDirectories.Length, toDirectories.Length);
            var lastCommonRoot = -1;
            // find common root
            for (int x = 0; x < length; x++)
            {
                if (string.Compare(fromDirectories[x], toDirectories[x], StringComparison.OrdinalIgnoreCase) != 0)
                    break;
                lastCommonRoot = x;
            }
            if (lastCommonRoot == -1)
            {
                throw new ArgumentException(
                    $"The paths '{fromDirectory} and '{toPath}' do not have a common prefix path.");
            }
            // add relative folders in from path
            for (var x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
                if (fromDirectories[x].Length > 0)
                    relativePath.Add("..");
            // add to folders to path
            for (var x = lastCommonRoot + 1; x < toDirectories.Length; x++)
                relativePath.Add(toDirectories[x]);
            // create relative path
            var relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);
            var newPath = string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
            return newPath;
        }

        /// <summary>
        /// Checks a string, whether it consists of pure seven bit characters.
        /// </summary>
        /// <param name="text">string text</param>
        /// <returns>true, if text only contains seven bit characters - else false.</returns>
        public static bool IsSevenBit(string text)
        {
            return text.All(t => t <= 127);
        }

        /// <summary>
        /// Checks a Stream, whether it consists of pure seven bit bytes.
        /// Assuming UTF8 encoding of the stream.
        /// </summary>
        /// <param name="stream">System.IO.Stream stream</param>
        /// <returns>true, if Stream only contains seven bit bytes - else false.</returns>
        public static bool IsSevenBit(Stream stream)
        {
            return IsSevenBit(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Checks a Stream, whether it consists of pure seven bit bytes.
        /// </summary>
        /// <param name="stream">System.IO.Stream stream</param>
        /// <param name="characterEncoding">System.Text.Encoding encoding</param>
        /// <returns>true, if Stream only contains seven bit characters - else false.</returns>
        public static bool IsSevenBit(Stream stream, Encoding characterEncoding)
        {
            return IsSevenBit(Stream2String(stream, characterEncoding));
        }


        /// <summary>
        /// Converts a stream to a string using UTF8 Encoding.
        /// </summary>
        /// <param name="stream">System.IO.Stream stream</param>
        /// <returns>string representation of the stream.</returns>
        public static string Stream2String(Stream stream)
        {
            return Stream2String(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Converts a stream to a string.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string Stream2String(Stream stream, Encoding encoding)
        {
            long streamPos = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int) stream.Length);
            stream.Seek(streamPos, SeekOrigin.Begin);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Wraps lines of text not exceeding the maximum length given.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string WrapLines(string input, int length)
        {
            var result = new StringBuilder();
            string[] lines = input.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            foreach (string line in lines)
            {
                result.Append(WrapLine(line, length));
            }
            return result.ToString();
        }

        /// <summary>
        /// Wraps a single of text not exceeding the maximum length given.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string WrapLine(string input, int length)
        {
            var result = new StringBuilder();
            while ((input.Length > length))
            {
                //  find the position of the last space before the length
                int cutPos = input.Substring(0, length).LastIndexOf(" ");
                if ((cutPos == -1))
                {
                    //  need to cut right at length
                    result.Append(input.Substring(0, length) + Environment.NewLine);
                    input = input.Substring((length + 1), (input.Length - (length + 1)));
                }
                else
                {
                    result.Append(input.Substring(0, cutPos) + Environment.NewLine);
                    input = input.Substring((cutPos + 1), (input.Length - (cutPos + 1)));
                }
            }
            if ((input.Length > 0))
            {
                result.Append(input + Environment.NewLine);
            }
            else
            {
                // Add newline if input is empty (added 2008-01-12 by NB)
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        /// <summary>
        /// Parses the email address and breaks into display name and address part.
        /// </summary>
        /// <remarks>
        /// Method is maintained because of compatibility reasons and it's only a wrapper for
        /// MimeKit.MailboxAddress.Parse(...).
        /// </remarks>
        /// <param name="inputAddr"></param>
        /// <param name="displayName"></param>
        /// <param name="address"></param>
        public static void ParseMailAddress(string inputAddr, out string displayName, out string address)
        {
            var mbAddr = MailboxAddress.Parse(ParserOptions.Default, inputAddr);
            displayName = mbAddr.Name;
            address = mbAddr.Address;
        }

        /// <summary>
        /// Calculates the size of a MimeMessage
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>Size of a message in bytes.</returns>
        /// <remarks>
        /// The ContentIds and Boundaries of a MimeMessage will change each time the message is written, so the size may differ for a few bytes.
        /// </remarks>
        public static long CalcMessageSize(MimeMessage msg)
        {
            return msg?.ToString().Length ?? 0;
        }


        /// <summary>
        /// Gets the header encoding (aka mime encoding) for the Encoding.
        /// </summary>
        /// <remarks>
        /// Required, because Encoding.HeaderName is not supported in .Net Core.
        /// </remarks>
        /// <param name="encoding"></param>
        /// <returns></returns>
        internal static string GetMimeCharset(Encoding encoding)
        {
            // This method is part of CharsetUtils.cs of MimeKit
            // Author: Jeffrey Stedfast <jeff@xamarin.com>
            // Copyright (c) 2013-2016 Xamarin Inc.

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            
            // There are only these 4 differences between Encoding.HeaderName and Encoding.WebName
            switch (encoding.CodePage)
            {
#if NETFRAMEWORK
                case 932: return "iso-2022-jp"; // shift_jis
                case 50221: return "iso-2022-jp"; // csISO2022JP
                case 949: return "euc-kr";      // ks_c_5601-1987
                case 50225: return "euc-kr";      // iso-2022-kr
#endif
                default:
                    return encoding.WebName.ToLowerInvariant();
            }
        }
    }
}