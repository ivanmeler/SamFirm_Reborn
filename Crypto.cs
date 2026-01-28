using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
  internal class Crypto
  {
    private static readonly byte[] IV = new byte[16]; // AES requires 16 byte IV for CBC, but here it was used as ECB with 1 byte?
    // Actually RijndaelManaged with ECB mode doesn't use IV.
    // In the original code it was new byte[1].
    public static Form1 form;
    private static byte[] KEY;

    public static int Decrypt(string encryptedFile, string outputFile, bool gui = true)
    {
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
      using (FileStream fileStream2 = new FileStream(outputFile, FileMode.Create))
      using (Aes aes = Aes.Create())
      {
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Crypto.KEY;
        using (ICryptoTransform decryptor = aes.CreateDecryptor())
        {
          try
          {
            Utility.PreventDeepSleep(Utility.PDSMode.Start);
            using (CryptoStream cryptoStream = new CryptoStream(fileStream1, decryptor, CryptoStreamMode.Read))
            {
              byte[] buffer = new byte[256 * 1024];
              long bytesRead = 0;
              int count;
              while ((count = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
              {
                Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                bytesRead += count;
                fileStream2.Write(buffer, 0, count);
                if (gui)
                  Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, fileStream1.Length), bytesRead);
                else
                  CmdLine.SetProgress(Utility.GetProgress(bytesRead, fileStream1.Length));
              }
            }
          }
          catch (CryptographicException)
          {
            Logger.WriteLog("Error decrypting file: Wrong key.", false);
            return 3;
          }
          catch (TargetInvocationException)
          {
            Logger.WriteLog("Error decrypting file: Please turn off FIPS compliance checking.", false);
            return 800;
          }
          catch (IOException ex)
          {
            Logger.WriteLog($"Error decrypting file: IOException: {ex.Message}", false);
            return 3;
          }
          finally
          {
            Utility.PreventDeepSleep(Utility.PDSMode.Stop);
          }
        }
      }
      return 0;
    }

    public static int DecryptAndUnzip(string encryptedFile, string outputDirectory, bool gui = true)
    {
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
      using (Aes aes = Aes.Create())
      {
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Crypto.KEY;
        using (ICryptoTransform decryptor = aes.CreateDecryptor())
        {
          try
          {
            Utility.PreventDeepSleep(Utility.PDSMode.Start);
            using (CryptoStream cryptoStream = new CryptoStream(fileStream1, decryptor, CryptoStreamMode.Read))
            {
              Logger.WriteLog("Please note that the sum of unzipped files might be larger than the downloaded firmware file", false);
              using (ZipInputStream s = new ZipInputStream(cryptoStream, 256 * 1024))
              {
                ZipEntry entry;
                byte[] data = new byte[256 * 1024];
                long bytesRead = 0;
                long fileSize = fileStream1.Length;
                while ((entry = s.GetNextEntry()) != null)
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  if (entry.IsFile)
                  {
                    if (entry.CanDecompress)
                    {
                      fileSize -= entry.CompressedSize;
                      fileSize += entry.Size;
                      string outputFile = Path.Combine(outputDirectory, entry.Name);
                      string directory = Path.GetDirectoryName(outputFile);
                      if (!Directory.Exists(directory))
                      {
                        Logger.WriteLog($"Creating directory {directory}", false);
                        Directory.CreateDirectory(directory);
                      }

                      Logger.WriteLog($"Writing file {outputFile}", false);
                      using (FileStream fileStream2 = new FileStream(outputFile, FileMode.Create))
                      {
                        int size;
                        while ((size = s.Read(data, 0, data.Length)) > 0)
                        {
                          bytesRead += size;
                          fileStream2.Write(data, 0, size);
                          if (gui)
                            Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, Math.Max(fileSize, bytesRead)), bytesRead);
                          else
                            CmdLine.SetProgress(Utility.GetProgress(bytesRead, Math.Max(fileSize, bytesRead)));
                        }
                      }
                      try
                      {
                        File.SetLastWriteTime(outputFile, entry.DateTime);
                      }
                      catch { }
                    }
                    else
                    {
                      bytesRead += entry.Size;
                      if (gui)
                        Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, Math.Max(fileSize, bytesRead)), bytesRead);
                      else
                        CmdLine.SetProgress(Utility.GetProgress(bytesRead, Math.Max(fileSize, bytesRead)));
                    }
                  }
                }
              }
            }
          }
          catch (CryptographicException)
          {
            Logger.WriteLog("Error decrypting file: Wrong key.", false);
            return 3;
          }
          catch (TargetInvocationException)
          {
            Logger.WriteLog("Error decrypting file: Please turn off FIPS compliance checking.", false);
            return 800;
          }
          catch (IOException ex)
          {
            Logger.WriteLog($"Error decrypting file: IOException: {ex.Message}", false);
            return 3;
          }
          catch (Exception ex)
          {
            Logger.WriteLog($"Error decrypting file: Exception: {ex.Message}", false);
            return 3;
          }
          finally
          {
            Utility.PreventDeepSleep(Utility.PDSMode.Stop);
          }
        }
      }
      return 0;
    }

    public static void SetDecryptKey(string region, string model, string version)
    {
      string keyStr = $"{region}:{model}:{version}";
      byte[] bytes = Encoding.ASCII.GetBytes(keyStr);
      using (MD5 md5 = MD5.Create())
        Crypto.KEY = md5.ComputeHash(bytes);
    }

    public static void SetDecryptKey(string version, string logicValue)
    {
      byte[] bytes = Encoding.ASCII.GetBytes(Utility.GetLogicCheck(version, logicValue));
      using (MD5 md5 = MD5.Create())
        Crypto.KEY = md5.ComputeHash(bytes);
    }
  }
}
