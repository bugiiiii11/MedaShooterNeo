using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class DataEncryption
{
    public static readonly string PUBLIC_KEY = @"<RSAKeyValue><Modulus>1dFiuBdrPpvaisfPb34cXlOSRPab/JU9t5WsFcIquTOhtTfOlMzCy53bT/UfjU2tyxZrb2d91QKWL8xfvRCfrFWo55lP9vst2QmoTz4tJgmp5NU+25t3kTA9rQ7zCwGPmnR5N5zsbbIFY2mMu0eN5pgwlB9HcSt+Xjd2W4d7rJc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
    public static readonly string GLOB_PUBLIC_KEY = @"<RSAKeyValue><Modulus>0HSXq7Hp1kD1U/edl5B27Mp75C6swRHuwrtMvwuwdZD3kJXrdBNivUiVNI0dULPkpRPqtOz6zSd7jMx/s5zM2eRpuqU5q/Qow5VpTDD+sYq1RhvXGiD+wX3yi9b0eqUl5FWCy42bVRtIPdhAIqO74RPR9YRCoePOOs4rfi1uRlE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    internal static uint EncryptScore(uint x)
    {
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = ((x >> 16) ^ x) * 0x45d9f3b;
        x = (x >> 16) ^ x;
        return x;
    }

    internal static uint DecryptScore(uint x)
    {
        x = ((x >> 16) ^ x) * 0x119de1f3;
        x = ((x >> 16) ^ x) * 0x119de1f3;
        x = (x >> 16) ^ x;
        return x;
    }
}

public class Asymmetric
{
    public class RSA
    {
        /// <summary>
        /// Encrypt data using a public key.
        /// </summary>
        /// <param name="bytes">Bytes to encrypt.</param>
        /// <param name="publicKey">Public key to use.</param>
        /// <returns>Encrypted data.</returns>
        public static byte[] Encrypt(byte[] bytes, string publicKey)
        {
            var csp = new CspParameters
            {
                ProviderType = 1,
            };

            using var rsa = new RSACryptoServiceProvider();

            rsa.FromXmlString(publicKey);
            var data = rsa.Encrypt(bytes, false);

            rsa.PersistKeyInCsp = false;

            return data;
        }

        internal static object Encrypt(object p, string gLOB_PUBLIC_KEY)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encrypt data using a public key.
        /// </summary>
        /// <param name="input">Data to encrypt.</param>
        /// <param name="publicKey">Public key to use.</param>
        /// <returns>Encrypted data.</returns>
        public static string Encrypt(string input, string publicKey)
        {
            if (input == null)
            {
                throw new Exception("Input cannot be null");
            }

            return Convert.ToBase64String(
                Encrypt(
                    Encoding.UTF8.GetBytes(input),
                    publicKey));
        }

        /// <summary>
        /// Decrypt data using a private key.
        /// </summary>
        /// <param name="bytes">Bytes to decrypt.</param>
        /// <param name="privateKey">Private key to use.</param>
        /// <returns>Decrypted data.</returns>
        public static byte[] Decrypt(byte[] bytes, string privateKey)
        {
            var csp = new CspParameters
            {
                ProviderType = 1
            };

            using var rsa = new RSACryptoServiceProvider(csp);

            rsa.FromXmlString(privateKey);
            var data = rsa.Decrypt(bytes, false);

            rsa.PersistKeyInCsp = false;

            return data;
        }

        /// <summary>
        /// Decrypt data using a private key.
        /// </summary>
        /// <param name="input">Base64 data to decrypt.</param>
        /// <param name="privateKey">Private key to use.</param>
        /// <returns>Decrypted data.</returns>
        public static string Decrypt(string input, string privateKey)
        {
            if (input == null)
            {
                throw new Exception("Input cannot be null");
            }

            return Encoding.UTF8.GetString(
                Decrypt(
                    Convert.FromBase64String(input),
                    privateKey));
        }
    }
}
