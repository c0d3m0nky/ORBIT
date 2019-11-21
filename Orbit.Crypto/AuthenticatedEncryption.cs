using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Orbit.Experimental
{
    // based on code at https://gist.github.com/jbtule/4336842#file-aesthenhmac-cs
    // ToDo: Make this fit with Cipher
    public static class AuthenticatedEncryption
    {
        private static readonly Dictionary<Type, Func<SymmetricAlgorithm>> Constructors
            = typeof(SymmetricAlgorithm).ImplementedBy().Select(t =>
                {
                    Func<SymmetricAlgorithm> constructor = null;
                    var method = t.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(c => c.Name == "Create" && !c.GetParameters().Any() && c.ReturnType == typeof(SymmetricAlgorithm));

                    if (method != null) constructor = () => (SymmetricAlgorithm) method.Invoke(null, null);

                    return new {type = t, constructor};
                })
                .Where(t => t.constructor != null)
                .ToDictionary(t => t.type, t => t.constructor);

        public static string Encrypt(string message, string cryptKey, string authKey)
            => Encrypt<Aes>(message, cryptKey, authKey);
        
        public static string Encrypt<T>(string message, string cryptKey, string authKey) where T : SymmetricAlgorithm
        {
            var constructor = Constructors.GetValueOrDefault(typeof(T));
            
            if(constructor==null) throw new Exception($"Constructor for {typeof(T).FullName} not found");
            
            var keyBitSize = 256;
            var blockBitSize = 128;

            var cryptKeyBytes = Convert.FromBase64String(cryptKey ?? string.Empty);
            if (cryptKeyBytes == null || cryptKeyBytes.Length != keyBitSize / 8)
            {
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", keyBitSize), nameof(cryptKey));
            }

            var authKeyBytes = Convert.FromBase64String(authKey ?? string.Empty);
            if (authKeyBytes == null || authKeyBytes.Length != keyBitSize / 8)
            {
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", keyBitSize), nameof(authKey));
            }

            if (message == null || message.Length < 1)
            {
                throw new ArgumentException("Message Required!", nameof(message));
            }

            byte[] cipherText;
            byte[] iv;

            using (var aes = constructor())
            {
                aes.KeySize = keyBitSize;
                aes.BlockSize = blockBitSize;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                aes.GenerateIV();
                iv = aes.IV;

                using (var encrypter = aes.CreateEncryptor(cryptKeyBytes, iv))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        binaryWriter.Write(Encoding.Unicode.GetBytes(message));
                    }

                    cipherText = cipherStream.ToArray();
                }
            }

            using (var hmac = new HMACSHA256(authKeyBytes))
            using (var encryptedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(encryptedStream))
                {
                    binaryWriter.Write(iv);
                    binaryWriter.Write(cipherText);
                    binaryWriter.Flush();

                    var tag = hmac.ComputeHash(encryptedStream.ToArray());
                    binaryWriter.Write(tag);
                }

                return Convert.ToBase64String(encryptedStream.ToArray());
            }
        }

        public static string Decrypt(string encrypted, string cryptKey, string authKey)
            => Decrypt<Aes>(encrypted, cryptKey, authKey);
        
        public static string Decrypt<T>(string encrypted, string cryptKey, string authKey)
        {
            var constructor = Constructors.GetValueOrDefault(typeof(T));
            
            if(constructor==null) throw new Exception($"Constructor for {typeof(T).FullName} not found");
            
            var keyBitSize = 256;
            var blockBitSize = 128;

            var cryptKeyBytes = Convert.FromBase64String(cryptKey ?? string.Empty);
            if (cryptKeyBytes == null || cryptKeyBytes.Length != keyBitSize / 8)
            {
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", keyBitSize), nameof(cryptKey));
            }

            var authKeyBytes = Convert.FromBase64String(authKey ?? string.Empty);
            if (authKeyBytes == null || authKeyBytes.Length != keyBitSize / 8)
            {
                throw new ArgumentException(string.Format("Key needs to be {0} bit!", keyBitSize), nameof(authKey));
            }

            if (encrypted == null || encrypted.Length == 0)
            {
                throw new ArgumentException("Encrypted Required!", nameof(encrypted));
            }

            var encryptedMessage = Convert.FromBase64String(encrypted);

            using (var hmac = new HMACSHA256(authKeyBytes))
            {
                var sentTag = new byte[hmac.HashSize / 8];

                var calcTag = hmac.ComputeHash(encryptedMessage, 0, encryptedMessage.Length - sentTag.Length);
                var ivLength = (blockBitSize / 8);

                //if message length is to small just return null
                if (encryptedMessage.Length < sentTag.Length + ivLength)
                {
                    return null;
                }

                Array.Copy(encryptedMessage, encryptedMessage.Length - sentTag.Length, sentTag, 0, sentTag.Length);

                //Compare Tag with constant time comparison
                var compare = 0;
                for (var i = 0; i < sentTag.Length; i++)
                {
                    compare |= sentTag[i] ^ calcTag[i];
                }

                //if message doesn't authenticate return null
                if (compare != 0)
                {
                    return null;
                }

                using (var aes = constructor())
                {
                    aes.KeySize = keyBitSize;
                    aes.BlockSize = blockBitSize;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;

                    var iv = new byte[ivLength];
                    Array.Copy(encryptedMessage, 0, iv, 0, iv.Length);

                    using (var decrypter = aes.CreateDecryptor(cryptKeyBytes, iv))
                    using (var plainTextStream = new MemoryStream())
                    {
                        using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                        using (var binaryWriter = new BinaryWriter(decrypterStream))
                        {
                            binaryWriter.Write(
                                encryptedMessage,
                                iv.Length,
                                encryptedMessage.Length - iv.Length - sentTag.Length
                            );
                        }

                        return Encoding.Unicode.GetString(plainTextStream.ToArray());
                    }
                }
            }
        }
    }
}