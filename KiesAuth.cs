using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
    public static class KiesAuth
    {
        private const string KEY_1 = "vicopx7dqu06emacgpnpy8j8zwhduwlh";

        private const string KEY_2 = "9u7qab84rpc16gvk";

        public static byte[] DecryptNonce(string inp)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] bytes = Convert.FromBase64String(inp);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                byte[] key = Encoding.UTF8.GetBytes(KEY_1);
                byte[] iv = key.Take(16).ToArray();
                aes.Key = key;
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor(key, iv))
                {
                    return decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }
        }

        public static string GetAuth(byte[] nonce)
        {
            var keydata = nonce.Select(c => (int)c % 16).ToArray();
            var fkey = GetFKey(keydata);
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                byte[] iv = fkey.Take(16).ToArray();
                using (var encryptor = aes.CreateEncryptor(fkey, iv))
                {
                    byte[] auth = encryptor.TransformFinalBlock(nonce, 0, nonce.Length);
                    return Convert.ToBase64String(auth);
                }
            }
        }

        public static byte[] GetFKey(int[] inp)
        {
            StringBuilder key = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                key.Append(KEY_1[inp[i]]);
            }
            key.Append(KEY_2);
            return Encoding.UTF8.GetBytes(key.ToString());
        }
    }
}
