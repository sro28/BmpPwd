﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace mrousavy {
    namespace Cryptography {
        /// <summary>
        /// Example of <see cref="ICrypt"/> (Cipher En/De-crypt Text with Passphrases)
        /// </summary>
        public class Cipher : ICrypt {
            private const int Keysize = 256;
            private const int DerivationIterations = 1000;

            /// <summary>
            /// Encrypt a Text
            /// </summary>
            /// <param name="unencryptedText">The text to Encrypt</param>
            /// <param name="salt">Salt for encryption</param>
            /// <returns>Encrypted Text</returns>
            public string Encrypt(string salt, string unencryptedText) {
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(unencryptedText);
                using(var password = new Rfc2898DeriveBytes(salt, saltStringBytes, DerivationIterations)) {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using(var symmetricKey = new RijndaelManaged()) {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using(var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes)) {
                            using(var memoryStream = new MemoryStream()) {
                                using(var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Decrypt a Text
            /// </summary>
            /// <param name="encryptedText">The text to Decrypt</param>
            /// <param name="salt">Salt for decryption</param>
            /// <returns>Decrypted Text</returns>
            public string Decrypt(string salt, string encryptedText) {
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(encryptedText);
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using(var password = new Rfc2898DeriveBytes(salt, saltStringBytes, DerivationIterations)) {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using(var symmetricKey = new RijndaelManaged()) {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using(var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) {
                            using(var memoryStream = new MemoryStream(cipherTextBytes)) {
                                using(var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)) {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy() {
                var randomBytes = new byte[32];
                using(var rngCsp = new RNGCryptoServiceProvider()) {
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
            }
        }
    }
}
