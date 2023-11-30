using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HoneyInPacifier.Utils
{
    public class DatabaseCrypt
    {
        private readonly static string _defaultPassword = ConfigurationManager.AppSettings["Application.Database.Password"].ToString();

        public static string HashMD5(string value)
        {
            byte[] data;
            StringBuilder sBuilder;

            using (MD5 md5Hash = MD5.Create())
            {
                data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            }

            sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static string HashSHA256(string value)
        {
            byte[] data;
            StringBuilder sBuilder;

            using (SHA256 md5Hash = SHA256.Create())
            {
                data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            }

            sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static string HashSHA512(string value)
        {
            byte[] data;
            StringBuilder sBuilder;

            using (SHA512 md5Hash = SHA512.Create())
            {
                data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            }

            sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static byte[] Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            byte[] saltBytes = new byte[] { 0x1E, 0x03, 0x14, 0x07, 0x16, 0x0C, 0x14, 0x08, 0x14, 0x01, 0x13, 0x59, 0x10, 0x0B, 0x13, 0x59 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    using (Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000))
                    {
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                    }

                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public static byte[] Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            byte[] saltBytes = new byte[] { 0x1E, 0x03, 0x14, 0x07, 0x16, 0x0C, 0x14, 0x08, 0x14, 0x01, 0x13, 0x59, 0x10, 0x0B, 0x13, 0x59 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    using (Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000))
                    {
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                    }

                    AES.Mode = CipherMode.CBC;

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        /// <summary>
        /// Criptografa com senha pessoal.
        /// </summary>
        /// <param name="value">
        /// Parâmetro value requer angrumento tipo string
        /// </param>
        /// <returns>
        /// Uma string criptografada
        /// </returns>
        public static string Encrypt(string value)
        {
            return AESEncrypt(value, _defaultPassword);
        }

        public static string AESEncrypt(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256

            using (SHA512 md5Hash = SHA512.Create())
            {
                passwordBytes = md5Hash.ComputeHash(passwordBytes);
            }

            byte[] bytesEncrypted = Encrypt(bytesToBeEncrypted, passwordBytes);

            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }

        /// <summary>
        /// Descriptografa a senha pessoal.
        /// </summary>
        /// <param name="value">
        /// Parâmetro value requer angrumento tipo string
        /// </param>
        /// <returns>
        /// Uma string string Descriptografada
        /// </returns>
        public static string Decrypt(string value)
        {
            return AESDecrypt(value, _defaultPassword);
        }

        public static string AESDecrypt(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            using (SHA512 md5Hash = SHA512.Create())
            {
                passwordBytes = md5Hash.ComputeHash(passwordBytes);
            }

            byte[] bytesDecrypted = Decrypt(bytesToBeDecrypted, passwordBytes);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            return result;
        }
    }
}