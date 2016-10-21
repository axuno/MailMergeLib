using System;
using System.Security.Cryptography;
using System.Text;

namespace MailMergeLib
{
	/// <summary>
	/// Simple encryption. Used to encrypt security relevant entries in configuration files.
	/// This is not "safe", but better than storing e.g. network credentials as plain text.
	/// </summary>
	public static class Crypto
	{
		// The Initialization Vector for the DES encryption routine. You may change this value, but keep the 8 bytes.
		private static readonly byte[] _Iv = new byte[8] { 255, 33, 128, 49, 0, 76, 177, 155 };

		/// <summary>
		/// The crypto key used to calculate an MD5 hash. Change this value before using the Crypto class.
		/// </summary>
		public static string CryptoKey { get; set; } = "MailMergeLibCrypt";

		/// <summary>
		/// Encoding used to encrypt or decrypt. Defaults to Encoding.UTF8
		/// </summary>
		public static Encoding Encoding { get; set; } = Encoding.UTF8;

		/// <summary>
		/// Encrypts the string parameter.
		/// </summary>
		/// <param name="s">The string to encrypt.</param>
		/// <returns>Returns the encrypted and base64 encoded string.</returns>
		public static string Encrypt(string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;
		
			var buffer = Encoding.GetBytes(s);
			var des = TripleDES.Create();
			des.Key = MD5.Create().ComputeHash(Encoding.GetBytes(CryptoKey));
			des.IV = _Iv;

			return Convert.ToBase64String(des.CreateEncryptor().TransformFinalBlock(buffer, 0, buffer.Length));
		}

		/// <summary>
		/// Decrypts the string parameter
		/// </summary>
		/// <param name="s">The base64 encoded, encrypted string.</param>
		/// <returns>Returns the decrypted, encoded string.</returns>
		public static string Decrypt(string s)
		{
			if (string.IsNullOrEmpty(s))
				return s;

			var buffer = Convert.FromBase64String(s);
			var des = TripleDES.Create();
			des.Key = MD5.Create().ComputeHash(Encoding.GetBytes(CryptoKey));
			des.IV = _Iv;

			return Encoding.GetString(des.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length));
		}
	}
}