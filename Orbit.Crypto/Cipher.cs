using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Orbit.Experimental;

namespace Orbit.Experimental
{
    // ToDo: This works, but needs work to reach maturity, I'm still learning best practices around this
    public class Cipher<T> : IDisposable where T : SymmetricAlgorithm
    {
        private static readonly Dictionary<Type, Func<SymmetricAlgorithm>> Constructors
            = typeof(SymmetricAlgorithm).ImplementedBy().Select(t =>
                {
                    Func<SymmetricAlgorithm> constructor = null;
                    var method = t.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .FirstOrDefault(c => c.Name == "Create" && !c.GetParameters().Any() && c.ReturnType == typeof(SymmetricAlgorithm));

                    if (method != null) constructor = () => (SymmetricAlgorithm) method.Invoke(null, null);

                    return new {type = t, constructor};
                })
                .Where(t => t.constructor != null)
                .ToDictionary(t => t.type, t => t.constructor);

        private readonly SymmetricAlgorithm _algorithm;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        protected Cipher(byte[] key, byte[] iv, CipherMode cipherMode, PaddingMode paddingMode, Func<SymmetricAlgorithm> algorithmFactory = null)
        {
            _algorithm = (algorithmFactory ?? Constructors.GetValueOrDefault(typeof(T)))?.Invoke();

            if (_algorithm == null) throw new Exception($"Constructor for {typeof(T).FullName} not found");

            _algorithm.Key = key;
            // ToDo: support letting the algorithm generate it
            _algorithm.IV = iv;
            _algorithm.Mode = cipherMode;
            _algorithm.Padding = paddingMode;

            _encryptor = _algorithm.CreateEncryptor();
            _decryptor = _algorithm.CreateDecryptor();
        }

        public byte[] Encrypt(byte[] dataBytes) => _encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        public byte[] Decrypt(byte[] encryptedDataBytes) => _decryptor.TransformFinalBlock(encryptedDataBytes, 0, encryptedDataBytes.Length);

        public virtual void Dispose()
        {
            _encryptor.Dispose();
            _decryptor.Dispose();
            ((IDisposable) _algorithm).Dispose();
        }

        public static string Encrypt<T>(string message, string base64Key, string base64Iv, Encoding encoding = null, bool webSafeEncode = false, CipherMode cipherMode = CipherMode.CBC,
            PaddingMode paddingMode = PaddingMode.PKCS7)
            where T : SymmetricAlgorithm
        {
            using (var ciph = new Cipher<T>((base64Key ?? string.Empty).Base64Decode(), (base64Iv ?? string.Empty).Base64Decode(), cipherMode, paddingMode))
            {
                return ciph.Encrypt((encoding ?? Encoding.UTF8).GetBytes(message)).Base64Encode(webSafeEncode);
            }
        }

        public static string Decrypt<T>(string message, string base64Key, string base64Iv, Encoding encoding = null, CipherMode cipherMode = CipherMode.CBC,
            PaddingMode paddingMode = PaddingMode.PKCS7)
            where T : SymmetricAlgorithm
        {
            using (var ciph = new Cipher<T>((base64Key ?? string.Empty).Base64Decode(), (base64Iv ?? string.Empty).Base64Decode(), cipherMode, paddingMode))
            {
                return (encoding ?? Encoding.UTF8).GetString(ciph.Decrypt(message.Base64Decode()));
            }
        }
    }
}