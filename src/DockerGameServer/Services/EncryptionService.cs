using System.Security.Cryptography;
using System.Text;

namespace DockerGameServer.Services
{
    public class EncryptionResult
    {
        public byte[] EncryptedValue { get; set; } = default!;
        public byte[] EncryptedDataKey { get; set; } = default!;
        public byte[] ValueNonce { get; set; } = default!;
        public byte[] DataKeyNonce { get; set; } = default!;
    }

    public class DecryptionResult
    {
        public byte[] Value { get; set; } = default!;
    }

    public class EncryptionService
    {
        private readonly byte[] _masterKey;
        private readonly string _hashPepper;

        public EncryptionService(IConfiguration configuration)
        {
            var base64 = configuration["Security:MasterKey"];
            if (base64 == null)
                throw new ArgumentNullException(nameof(base64));

            var Pepper = configuration["Security:HashPepper"];
            if (Pepper == null)
                throw new ArgumentNullException(nameof(Pepper));

            byte[] masterKey = Convert.FromBase64String(base64);
            if (masterKey == null || masterKey.Length != 32)
                throw new ArgumentException("Master key must be 256-bit (32 bytes).");

            _masterKey = masterKey;
            _hashPepper = Pepper;
        }

        // -----------------------------
        // ENCRYPT
        // -----------------------------
        public EncryptionResult Encrypt(byte[] value)
        {
            // 1. Generate random data key
            byte[] dataKey = RandomNumberGenerator.GetBytes(32);

            // 2. Encrypt value with data key (AES-GCM)
            byte[] valueNonce = RandomNumberGenerator.GetBytes(12);
            byte[] encryptedValue = AesGcmEncrypt(value, dataKey, valueNonce);

            // 3. Encrypt data key with master key (AES-GCM)
            byte[] dataKeyNonce = RandomNumberGenerator.GetBytes(12);
            byte[] encryptedDataKey = AesGcmEncrypt(dataKey, _masterKey, dataKeyNonce);

            return new EncryptionResult
            {
                EncryptedValue = encryptedValue,
                EncryptedDataKey = encryptedDataKey,
                ValueNonce = valueNonce,
                DataKeyNonce = dataKeyNonce
            };
        }

        // -----------------------------
        // DECRYPT
        // -----------------------------
        public DecryptionResult Decrypt(
            byte[] encryptedValue,
            byte[] encryptedDataKey,
            byte[] valueNonce,
            byte[] dataKeyNonce)
        {
            // 1. Decrypt data key using master key
            byte[] dataKey = AesGcmDecrypt(encryptedDataKey, _masterKey, dataKeyNonce);

            // 2. Decrypt value using data key
            byte[] value = AesGcmDecrypt(encryptedValue, dataKey, valueNonce);

            return new DecryptionResult
            {
                Value = value
            };
        }

        public string CreateLookupHash(string normalizedValue)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(normalizedValue + _hashPepper);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        // -----------------------------
        // AES-GCM helper methods
        // -----------------------------
        private static byte[] AesGcmEncrypt(byte[] plaintext, byte[] key, byte[] nonce)
        {
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(key);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Combine ciphertext + tag
            return ciphertext.Concat(tag).ToArray();
        }

        private static byte[] AesGcmDecrypt(byte[] encrypted, byte[] key, byte[] nonce)
        {
            int tagLength = 16;
            int ciphertextLength = encrypted.Length - tagLength;

            byte[] ciphertext = encrypted[..ciphertextLength];
            byte[] tag = encrypted[ciphertextLength..];

            byte[] plaintext = new byte[ciphertextLength];

            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
