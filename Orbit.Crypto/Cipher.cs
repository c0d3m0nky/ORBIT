using System;
using System.Security.Cryptography;

namespace Orbit
{
    public abstract class Cipher<T> : IDisposable where T : SymmetricAlgorithm
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly CipherMode _cipherMode;
        private readonly PaddingMode _paddingMode;
        private readonly SymmetricAlgorithm _algorithm;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        private Cipher()
        {
        }

        protected Cipher(byte[] key, byte[] iv, CipherMode cipherMode, PaddingMode paddingMode)
        {
            _key = key;
            _iv = iv;
            _cipherMode = cipherMode;
            _paddingMode = paddingMode;
            _algorithm = InstantiateAlgorithm();

            _algorithm.Key = _key;
            _algorithm.IV = _iv;
            _algorithm.Mode = _cipherMode;
            _algorithm.Padding = _paddingMode;

            _encryptor = _algorithm.CreateEncryptor();
            _decryptor = _algorithm.CreateDecryptor();
        }

        protected abstract T InstantiateAlgorithm();

        public byte[] Encrypt(byte[] dataBytes) => _encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        public byte[] Decrypt(byte[] encryptedDataBytes) => _decryptor.TransformFinalBlock(encryptedDataBytes, 0, encryptedDataBytes.Length);

        public string Base64Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').Replace('=', '~').Trim();
        }

        public byte[] Base64Decode(string data)
        {
            return Convert.FromBase64String(data.Replace('-', '+').Replace('_', '/').Replace('~', '='));
        }

        public virtual void Dispose()
        {
            _encryptor.Dispose();
            _decryptor.Dispose();
            ((IDisposable) _algorithm).Dispose();
        }
    }
}