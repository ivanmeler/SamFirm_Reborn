using ICSharpCode.SharpZipLib.Zip;
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
                byte[] buffer = new byte[256 * 1024];
                long bytesRead = 0;
                int count;
                do
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  bytesRead += (long) (count = cryptoStream.Read(buffer, 0, buffer.Length));
                  fileStream2.Write(buffer, 0, count);
                  if (GUI)
                    Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, fileStream1.Length), bytesRead);
                  else
                    CmdLine.SetProgress(Utility.GetProgress(bytesRead, fileStream1.Length));
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

    public static int DecryptAndUnzip(string encryptedFile, string outputDirectory, bool GUI = true)
    {
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
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
            using (CryptoStream cryptoStream = new CryptoStream((Stream)fileStream1, decryptor, CryptoStreamMode.Read))
            {
              Logger.WriteLog($"Please note that the sum of unzipped files might be larger than the downloaded firmware file", false);
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
                          if (GUI)
                            Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, System.Math.Max(fileSize, bytesRead)), bytesRead);
                          else
                            CmdLine.SetProgress(Utility.GetProgress(bytesRead, System.Math.Max(fileSize, bytesRead)));

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
                      if (GUI)
                        Crypto.form.SetProgressBar(Utility.GetProgress(bytesRead, System.Math.Max(fileSize, bytesRead)), bytesRead);
                      else
                        CmdLine.SetProgress(Utility.GetProgress(bytesRead, System.Math.Max(fileSize, bytesRead)));
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
            Logger.WriteLog("Error decrypting file: IOException: " + ex.Message, false);
            return 3;
          }
          catch (System.Exception ex)
          {
            Logger.WriteLog("Error decrypting file: Exception: " + ex.Message, false);
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
