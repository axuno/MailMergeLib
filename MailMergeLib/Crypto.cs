using System;
using System.Security.Cryptography;
using System.Text;

namespace MailMergeLib
{
    /// <summary>
    /// Simple encryption. Used to encrypt security relevant entries in configuration files.
    /// Encryption can be enabled/disabled by setting <see cref="Enabled"/>.
    /// </summary>
    public static class Crypto
    {
        /// <summary>
        /// Switches encryption on and off. Default is <c>off</c>.
        /// </summary>
        public static bool Enabled { get; set; } = false;

        /// <summary>
        /// The Initialization Vector for the DES encryption routine. You should change the default value, but keep the 8 bytes.
        /// </summary>
        public static byte[] IV { get; set; } = new byte[8] {255, 33, 128, 49, 0, 76, 177, 155};

        /// <summary>
        /// The crypto key used to calculate an MD5 hash. You should change the default value before using the Crypto class.
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
            if (!Enabled || string.IsNullOrEmpty(s))
                return s;
        
            var buffer = Encoding.GetBytes(s);
            using (var des = TripleDES.Create())
            {
                using (var md5 = MD5.Create())
                {
                    des.Key = md5.ComputeHash(Encoding.GetBytes(CryptoKey));
                    des.IV = IV;

                    return Convert.ToBase64String(des.CreateEncryptor().TransformFinalBlock(buffer, 0, buffer.Length));
                }
            }
        }

        /// <summary>
        /// Decrypts the string parameter
        /// </summary>
        /// <param name="s">The base64 encoded, encrypted string.</param>
        /// <returns>Returns the decrypted, encoded string.</returns>
        public static string Decrypt(string s)
        {
            if (!Enabled || string.IsNullOrEmpty(s))
                return s;

            var buffer = Convert.FromBase64String(s);

            using (var des = TripleDES.Create())
            {
                using (var md5 = MD5.Create())
                {
                    des.Key = md5.ComputeHash(Encoding.GetBytes(CryptoKey));
                    des.IV = IV;

                    return Encoding.GetString(des.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length));
                }
            }
        }
    }
}