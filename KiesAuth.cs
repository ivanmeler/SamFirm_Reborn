using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
    public static class KiesAuth
    {
        private const string KEY_1 = "hqzdurufm2c8mf6bsjezu1qgveouv7c7";

        private const string KEY_2 = "w13r4cvf4hctaujv";

        public static byte[] DecryptNonce(string inp)
        {
          using (AesManaged aes = new AesManaged())
          {
            byte[] bytes = Convert.FromBase64String(inp);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            byte[] key = Encoding.UTF8.GetBytes(KEY_1);
            byte[] iv = key.Take(16).ToArray();
            aes.Key = key;
            aes.IV = iv;
            var decryptor = aes.CreateDecryptor(key, iv);
            byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return decrypted;
          }
        }

        public static string GetAuth(byte[] nonce)
        {
            var keydata = nonce.Select(c => c % 16).ToArray();
            var fkey = get_fkey(keydata);
            using (AesManaged aes = new AesManaged())
            {
              aes.Mode = CipherMode.CBC;
              aes.Padding = PaddingMode.PKCS7;
              byte[] iv = fkey.Take(16).ToArray();
              var encryptor = aes.CreateEncryptor(fkey, iv);
              byte[] auth = encryptor.TransformFinalBlock(nonce, 0, nonce.Length);
              return Convert.ToBase64String(auth);
            }
        }

        public static byte[] get_fkey(int[] inp)
        {
            var key = "";
            for (int i = 0; i < 16; i++)
            {
                key += KEY_1[inp[i]];
            }
            key += KEY_2;
            return Encoding.UTF8.GetBytes(key);
        }

    }
}
