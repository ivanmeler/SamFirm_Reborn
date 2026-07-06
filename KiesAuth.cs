using System;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
    public static class KiesAuth
    {
        private static readonly byte[] AuthAesKey = new byte[]
        {
            0x42, 0x2e, 0x73, 0x73, 0x36, 0x17, 0xae, 0x2b,
            0x19, 0x89, 0x40, 0xfd, 0x4e, 0x32, 0xb0, 0xa5
        };

        public static byte[] DecryptNonce(string inp)
        {
            return Encoding.ASCII.GetBytes(inp);
        }

        public static string GetAuth(byte[] nonce)
        {
            byte[] block = Encoding.ASCII.GetBytes("0000000000000000");
            Buffer.BlockCopy(nonce, 0, block, 0, Math.Min(nonce.Length, block.Length));

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.Key = AuthAesKey;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] auth = encryptor.TransformFinalBlock(block, 0, block.Length);
                    StringBuilder hex = new StringBuilder(auth.Length * 2);
                    foreach (byte value in auth)
                        hex.Append(value.ToString("x2"));
                    return hex.ToString();
                }
            }
        }
    }
}
