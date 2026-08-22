using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm
{
  internal class Crypto
  {
    private const long ProgressUpdateBytes = 4L * 1024L * 1024L;
    private static readonly byte[] IV = new byte[16];
    public static Form1 form;
    private static byte[] KEY;
    private static long lastReportedBytes;
    private static int lastReportedProgress = -1;

    public static int Decrypt(string encryptedFile, string outputFile, bool GUI = true)
    {
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
      {
        using (FileStream fileStream2 = new FileStream(outputFile, FileMode.Create))
        {
          if (!ValidateDecryptKey())
            return 3;

          RijndaelManaged rijndaelManaged = new RijndaelManaged();
          rijndaelManaged.Mode = CipherMode.ECB;
          rijndaelManaged.BlockSize = 128;
          rijndaelManaged.Padding = PaddingMode.PKCS7;
          using (ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(Crypto.KEY, Crypto.IV))
          {
            try
            {
              Stopwatch sw = new Stopwatch();
              ResetProgress(GUI);
              Utility.PreventDeepSleep(Utility.PDSMode.Start);
              using (ProgressStream progressStream = new ProgressStream(fileStream1, processed =>
                ReportProgress(Math.Min(processed, fileStream1.Length), fileStream1.Length, GUI, sw)))
              using (CryptoStream cryptoStream = new CryptoStream(progressStream, decryptor, CryptoStreamMode.Read))
              {
                byte[] buffer = new byte[256 * 1024];
                int count;
                do
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  count = cryptoStream.Read(buffer, 0, buffer.Length);
                  fileStream2.Write(buffer, 0, count);
                  ReportProgress(Math.Min(fileStream1.Position, fileStream1.Length), fileStream1.Length, GUI, sw);
                }
                while (count > 0);
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
            finally
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Stop);
              ResetSpeedLabel(GUI);
            }
          }
        }
      }
      return 0;
    }

    public static int DecryptAndUnzip(string encryptedFile, string outputDirectory, bool GUI = true)
    {
      Logger.WriteLog("Opening encrypted file " + encryptedFile, false);
      using (FileStream fileStream1 = new FileStream(encryptedFile, FileMode.Open))
      {
        Logger.WriteLog("Encrypted file opened.", false);
        Logger.WriteLog("Encrypted file size: " + fileStream1.Length + " bytes", false);
        Logger.WriteLog("Preparing decryptor...", false);
        if (!ValidateDecryptKey())
          return 3;

        RijndaelManaged rijndaelManaged = new RijndaelManaged();
        rijndaelManaged.Mode = CipherMode.ECB;
        rijndaelManaged.BlockSize = 128;
        rijndaelManaged.Padding = PaddingMode.PKCS7;
        using (ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(Crypto.KEY, Crypto.IV))
        {
          Logger.WriteLog("Decryptor prepared.", false);
          try
          {
            Stopwatch sw = new Stopwatch();
            Logger.WriteLog("Resetting decrypt progress...", false);
            ResetProgress(GUI);
            Logger.WriteLog("Decrypt progress initialized.", false);
            Utility.PreventDeepSleep(Utility.PDSMode.Start);
            Logger.WriteLog("Initializing decrypt stream...", false);
            using (ProgressStream progressStream = new ProgressStream(fileStream1, processed =>
              ReportProgress(Math.Min(processed, fileStream1.Length), fileStream1.Length, GUI, sw)))
            using (CryptoStream cryptoStream = new CryptoStream(progressStream, decryptor, CryptoStreamMode.Read))
            {
              Logger.WriteLog("Please note that the sum of unzipped files might be larger than the downloaded firmware file", false);
              Logger.WriteLog("Reading firmware package entries...", false);
              using (ZipInputStream s = new ZipInputStream(cryptoStream, 256 * 1024))
              {
                ZipEntry entry;
                byte[] data = new byte[256 * 1024];
                int fileCount = 0;

                while ((entry = s.GetNextEntry()) != null)
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  ReportProgress(Math.Min(fileStream1.Position, fileStream1.Length), fileStream1.Length, GUI, sw);
                  if (entry.IsFile)
                  {
                    fileCount++;
                    if (entry.CanDecompress)
                    {
                      string outputFile = Path.Combine(outputDirectory, entry.Name);
                      string directory = Path.GetDirectoryName(outputFile);
                      if (!Directory.Exists(directory))
                      {
                        Logger.WriteLog("Creating directory " + directory, false);
                        Directory.CreateDirectory(directory);
                      }

                      Logger.WriteLog("Writing file " + outputFile, false);
                      using (FileStream fileStream2 = new FileStream(outputFile, FileMode.Create))
                      {
                        int size;
                        while ((size = s.Read(data, 0, data.Length)) > 0)
                        {
                          fileStream2.Write(data, 0, size);
                          ReportProgress(Math.Min(fileStream1.Position, fileStream1.Length), fileStream1.Length, GUI, sw);
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
                      ReportProgress(Math.Min(fileStream1.Position, fileStream1.Length), fileStream1.Length, GUI, sw);
                    }
                  }
                }
                if (fileCount == 0)
                  Logger.WriteLog("No files were found in the decrypted firmware package.", false);
                else
                  Logger.WriteLog("Finished reading firmware package entries. Files written: " + fileCount, false);
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
          catch (Exception ex)
          {
            Logger.WriteLog("Error decrypting file: Exception: " + ex.Message, false);
            return 3;
          }
          finally
          {
            Utility.PreventDeepSleep(Utility.PDSMode.Stop);
            ResetSpeedLabel(GUI);
          }
        }
      }
      return 0;
    }

    public static int Unzip(string decryptedFile, string outputDirectory, bool GUI = true)
    {
      try
      {
        Directory.CreateDirectory(outputDirectory);
        string root = Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar;
        using (FileStream input = new FileStream(decryptedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (ZipInputStream zip = new ZipInputStream(input, 256 * 1024))
        {
          ZipEntry entry;
          byte[] buffer = new byte[256 * 1024];
          while ((entry = zip.GetNextEntry()) != null)
          {
            if (!entry.IsFile)
              continue;

            string outputFile = Path.GetFullPath(Path.Combine(outputDirectory, entry.Name));
            if (!outputFile.StartsWith(root, StringComparison.OrdinalIgnoreCase))
              throw new IOException("Firmware archive contains an invalid path");
            string directory = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directory))
              Directory.CreateDirectory(directory);
            using (FileStream output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
              int count;
              while ((count = zip.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, count);
            }
            try { File.SetLastWriteTime(outputFile, entry.DateTime); } catch { }
          }
        }
        return 0;
      }
      catch (Exception ex)
      {
        Logger.WriteLog("Error unzipping firmware: " + ex.Message, false);
        return 3;
      }
    }

    private static void ResetProgress(bool GUI)
    {
      Utility.ResetSpeed(0);
      lastReportedBytes = 0;
      lastReportedProgress = -1;
      if (!GUI)
        return;

      Crypto.form.SetProgressBar(0, 0);
      ResetSpeedLabel(true);
    }

    private static void ReportProgress(long processedBytes, long totalBytes, bool GUI, Stopwatch sw)
    {
      if (totalBytes > 0)
        processedBytes = Math.Min(processedBytes, totalBytes);

      int progress = totalBytes <= 0 ? 0 : Utility.GetProgress(processedBytes, totalBytes);
      bool finalUpdate = totalBytes > 0 && processedBytes >= totalBytes;
      if (!finalUpdate && progress == lastReportedProgress && processedBytes - lastReportedBytes < ProgressUpdateBytes)
        return;

      lastReportedBytes = processedBytes;
      lastReportedProgress = progress;

      if (GUI)
      {
        Crypto.form.SetProgressBar(progress, processedBytes);
        int speed = Utility.DownloadSpeed(processedBytes, sw);
        if (speed != -1)
          Crypto.form.lbl_speed.Invoke((Delegate)((Action)(() => Crypto.form.lbl_speed.Text = speed.ToString() + " KB/s")));
      }
      else
        CmdLine.SetProgress(progress);
    }

    private static void ResetSpeedLabel(bool GUI)
    {
      if (!GUI)
        return;

      Crypto.form.lbl_speed.Invoke((Delegate)((Action)(() => Crypto.form.lbl_speed.Text = "0 KB/s")));
    }

    private static bool ValidateDecryptKey()
    {
      if (Crypto.KEY == null)
      {
        Logger.WriteLog("Error decrypting file: Decrypt key was not set.", false);
        return false;
      }

      if (Crypto.KEY.Length != 16 && Crypto.KEY.Length != 24 && Crypto.KEY.Length != 32)
      {
        Logger.WriteLog("Error decrypting file: Invalid decrypt key length: " + Crypto.KEY.Length, false);
        return false;
      }

      Logger.WriteLog("Decrypt key length: " + Crypto.KEY.Length + " bytes", false);
      return true;
    }

    public static ICryptoTransform CreateDecryptor()
    {
      if (!ValidateDecryptKey())
        return null;

      RijndaelManaged rijndaelManaged = new RijndaelManaged();
      rijndaelManaged.Mode = CipherMode.ECB;
      rijndaelManaged.BlockSize = 128;
      rijndaelManaged.Padding = PaddingMode.None;
      return rijndaelManaged.CreateDecryptor(Crypto.KEY, Crypto.IV);
    }

    private class ProgressStream : Stream
    {
      private readonly Stream inner;
      private readonly Action<long> progressCallback;

      public ProgressStream(Stream inner, Action<long> progressCallback)
      {
        this.inner = inner;
        this.progressCallback = progressCallback;
      }

      public override bool CanRead
      {
        get { return this.inner.CanRead; }
      }

      public override bool CanSeek
      {
        get { return this.inner.CanSeek; }
      }

      public override bool CanWrite
      {
        get { return false; }
      }

      public override long Length
      {
        get { return this.inner.Length; }
      }

      public override long Position
      {
        get { return this.inner.Position; }
        set { this.inner.Position = value; }
      }

      public override void Flush()
      {
        this.inner.Flush();
      }

      public override int Read(byte[] buffer, int offset, int count)
      {
        int read = this.inner.Read(buffer, offset, count);
        this.progressCallback(this.inner.Position);
        return read;
      }

      public override long Seek(long offset, SeekOrigin origin)
      {
        long position = this.inner.Seek(offset, origin);
        this.progressCallback(position);
        return position;
      }

      public override void SetLength(long value)
      {
        throw new NotSupportedException();
      }

      public override void Write(byte[] buffer, int offset, int count)
      {
        throw new NotSupportedException();
      }

      protected override void Dispose(bool disposing)
      {
        if (disposing)
          this.inner.Dispose();
        base.Dispose(disposing);
      }
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
