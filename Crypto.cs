// Decompiled with JetBrains decompiler
// Type: SamFirm.Crypto
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
  internal class Crypto
  {
    private static readonly byte[] IV = new byte[1];
    public static Form1 form;
    private static byte[] KEY;

    public static int Decrypt(string encryptedFile, string outputFile, bool GUI = true)
    {
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
      {
        using (FileStream fileStream2 = new FileStream(outputFile, FileMode.Create))
        {
          RijndaelManaged rijndaelManaged = new RijndaelManaged();
          rijndaelManaged.Mode = CipherMode.ECB;
          rijndaelManaged.BlockSize = 128;
          rijndaelManaged.Padding = PaddingMode.PKCS7;
          using (ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(Crypto.KEY, Crypto.IV))
          {
            try
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Start);
              using (CryptoStream cryptoStream = new CryptoStream((Stream) fileStream1, decryptor, CryptoStreamMode.Read))
              {
                byte[] buffer = new byte[4096];
                long num = 0;
                int count;
                do
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  num += (long) (count = cryptoStream.Read(buffer, 0, buffer.Length));
                  fileStream2.Write(buffer, 0, count);
                  if (GUI)
                    Crypto.form.SetProgressBar(Utility.GetProgress(num, fileStream1.Length));
                  else
                    CmdLine.SetProgress(Utility.GetProgress(num, fileStream1.Length));
                }
                while (count > 0);
              }
            }
            catch (CryptographicException ex)
            {
              Logger.WriteLog("Error decrypting file: Wrong key.", false);
              return 3;
            }
            catch (TargetInvocationException ex)
            {
              Logger.WriteLog("Error decrypting file: Please turn off FIPS compliance checking.", false);
              return 800;
            }
            catch (IOException ex)
            {
              Logger.WriteLog("Error decrypting file: IOException: " + ex.Message, false);
              return 3;
            }
            finally
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Stop);
            }
          }
        }
      }
      return 0;
    }

    public static void SetDecryptKey(string region, string model, string version)
    {
      StringBuilder stringBuilder = new StringBuilder(region);
      stringBuilder.Append(':');
      stringBuilder.Append(model);
      stringBuilder.Append(':');
      stringBuilder.Append(version);
      byte[] bytes = Encoding.ASCII.GetBytes(stringBuilder.ToString());
      using (MD5 md5 = MD5.Create())
        Crypto.KEY = md5.ComputeHash(bytes);
    }

    public static void SetDecryptKey(string version, string LogicValue)
    {
      byte[] bytes = Encoding.ASCII.GetBytes(Utility.GetLogicCheck(version, LogicValue));
      using (MD5 md5 = MD5.Create())
        Crypto.KEY = md5.ComputeHash(bytes);
    }
  }
}
