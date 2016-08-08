using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
				throw new ArgumentNullException("fromDirectory");
			if (toPath == null)
				throw new ArgumentNullException("toPath");
			if (Path.IsPathRooted(fromDirectory) && Path.IsPathRooted(toPath))
			{
				if (string.Compare(Path.GetPathRoot(fromDirectory),
				                   Path.GetPathRoot(toPath), true) != 0)
				{
					throw new ArgumentException(
						string.Format("The paths '{0} and '{1}' have different path roots.",
						              fromDirectory, toPath));
				}
			}
			var relativePath = new StringCollection();
			string[] fromDirectories = fromDirectory.Split(Path.DirectorySeparatorChar);
			string[] toDirectories = toPath.Split(Path.DirectorySeparatorChar);
			int length = Math.Min(fromDirectories.Length, toDirectories.Length);
			int lastCommonRoot = -1;
			// find common root
			for (int x = 0; x < length; x++)
			{
				if (string.Compare(fromDirectories[x], toDirectories[x], true) != 0)
					break;
				lastCommonRoot = x;
			}
			if (lastCommonRoot == -1)
			{
				throw new ArgumentException(
					string.Format("The paths '{0} and '{1}' do not have a common prefix path.",
					              fromDirectory, toPath));
			}
			// add relative folders in from path
			for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
				if (fromDirectories[x].Length > 0)
					relativePath.Add("..");
			// add to folders to path
			for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
				relativePath.Add(toDirectories[x]);
			// create relative path
			var relativeParts = new string[relativePath.Count];
			relativePath.CopyTo(relativeParts, 0);
			string newPath = string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
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
		/// </summary>
		/// <param name="stream">System.IO.Stream stream</param>
		/// <returns>true, if Stream only contains seven bit bytes - else false.</returns>
		public static bool IsSevenBit(Stream stream)
		{
			return IsSevenBit(stream, Encoding.Default);
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
		/// Converts a stream to a string using systems's current ANSI codepage.
		/// </summary>
		/// <param name="stream">System.IO.Stream stream</param>
		/// <returns>string representation of the stream.</returns>
		public static string Stream2String(Stream stream)
		{
			return Stream2String(stream, Encoding.Default);
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
		/// <param name="inputAddr"></param>
		/// <param name="displayName"></param>
		/// <param name="address"></param>
		public static void ParseMailAddress(string inputAddr, out string displayName, out string address)
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

			address = inputAddr.TrimStart(new[] {'<'}).TrimEnd(new[] {'>'}).Trim();

			if (address.IndexOf('@') == 0)
				address = null;
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
	}
}