using System;
using System.IO;
using System.Threading;

namespace SamFirm
{
  internal class CmdLine
  {
    private static string version = string.Empty;
    private static string file = string.Empty;
    private static string model = string.Empty;
    private static string region = string.Empty;
    private static string logicValue = string.Empty;
    private static string folder = string.Empty;
    private static bool binary = false;
    private static bool autodecrypt = false;
    private static string metafile = string.Empty;
    private static string fwdest = string.Empty;
    private static bool checkonly = false;
    public static CmdLine.ProgressBarInfo progressBar = new CmdLine.ProgressBarInfo();

    public static int Main(string[] args)
    {
      Thread.Sleep(200);
      if (CmdLine.InputValidation(args))
        return CmdLine.ProcessAction();
      CmdLine.DisplayUsage();
      return 1;
    }

    private static int ProcessAction()
    {
      int num = -1;
      if (!string.IsNullOrEmpty(CmdLine.file))
      {
        if (!string.IsNullOrEmpty(CmdLine.region) && !string.IsNullOrEmpty(CmdLine.model) && !string.IsNullOrEmpty(CmdLine.version))
          num = CmdLine.DoDecrypt();
        else if (!string.IsNullOrEmpty(CmdLine.logicValue) && !string.IsNullOrEmpty(CmdLine.version))
          num = CmdLine.DoDecrypt();
      }
      else if (!string.IsNullOrEmpty(CmdLine.model) && !string.IsNullOrEmpty(CmdLine.region))
        num = !CmdLine.checkonly ? CmdLine.DoDownload() : CmdLine.DoCheck();
      if (num == -1)
      {
        CmdLine.DisplayUsage();
        num = 1;
      }
      return num;
    }

    private static int DoDecrypt()
    {
      Logger.WriteLog("========== SamFirm Firmware Decrypter ==========\n", false);
      Logger.WriteLog("Decrypting file " + CmdLine.file + "...", false);
      CmdLine.CreateProgressbar();
      if (CmdLine.file.EndsWith(".enc2"))
        Crypto.SetDecryptKey(CmdLine.region, CmdLine.model, CmdLine.version);
      else if (CmdLine.file.EndsWith(".enc4"))
        Crypto.SetDecryptKey(CmdLine.version, CmdLine.logicValue);
      if (Crypto.Decrypt(CmdLine.file, Path.GetFileNameWithoutExtension(CmdLine.file), false) != 0)
      {
        Logger.WriteLog("\nError decrypting file", false);
        Logger.WriteLog("Please make sure the filename is not modified and verify the version / logicValue argument", false);
        File.Delete(Path.GetFileNameWithoutExtension(CmdLine.file));
        return 3;
      }
      Logger.WriteLog("\nDecrypting successful", false);
      return 0;
    }

    private static int DoCheck()
    {
      Logger.WriteLog("========== SamFirm Firmware Update Check ==========\n", false);
      Command.Firmware firmware;
      if (string.IsNullOrEmpty(CmdLine.version))
      {
        firmware = Command.UpdateCheckAuto(CmdLine.model, CmdLine.region, CmdLine.binary);
        if (firmware.FetchAttempts == 0)
          return 5;
      }
      else
        firmware = Command.UpdateCheck(CmdLine.model, CmdLine.region, CmdLine.version, CmdLine.binary, false);
      return firmware.Version == null ? 2 : 0;
    }

    private static int DoDownload()
    {
      Logger.WriteLog("========== SamFirm Firmware Downloader ==========\n", false);
      Command.Firmware fw;
      if (string.IsNullOrEmpty(CmdLine.version))
      {
        fw = Command.UpdateCheckAuto(CmdLine.model, CmdLine.region, CmdLine.binary);
        if (fw.FetchAttempts == 0)
          return 5;
      }
      else
        fw = Command.UpdateCheck(CmdLine.model, CmdLine.region, CmdLine.version, CmdLine.binary, false);
      if (fw.Version == null)
        return 2;
      string str = Path.Combine(CmdLine.folder, fw.Filename);
      Logger.WriteLog("Downloading...\n", false);
      CmdLine.CreateProgressbar();
      int num1;
      do
      {
        Utility.ReconnectCmdLine();
        Utility.ReconnectDownload = false;
        num1 = Command.Download(fw.Path, fw.Filename, fw.Version, fw.Region, fw.Model_Type, str, fw.Size, false);
      }
      while (Utility.ReconnectDownload);
      if (num1 != 200 && num1 != 206)
      {
        Logger.WriteLog("Error: " + (object) num1, false);
        return 4;
      }
      if (CmdLine.autodecrypt)
      {
        if (str.EndsWith(".enc2"))
          Crypto.SetDecryptKey(fw.Region, fw.Model, fw.Version);
        else if (str.EndsWith(".enc4"))
        {
          if (fw.BinaryNature == 1)
            Crypto.SetDecryptKey(fw.Version, fw.LogicValueFactory);
          else
            Crypto.SetDecryptKey(fw.Version, fw.LogicValueHome);
        }
        Logger.WriteLog("\nDecrypting...\n", false);
        CmdLine.CreateProgressbar();
        CmdLine.fwdest = Path.Combine(Path.GetDirectoryName(str), Path.GetFileNameWithoutExtension(fw.Filename));
        int num2 = Crypto.Decrypt(str, CmdLine.fwdest, false);
        File.Delete(str);
        if (num2 != 0)
        {
          File.Delete(CmdLine.fwdest);
          return 3;
        }
      }
      if (!string.IsNullOrEmpty(CmdLine.metafile))
        CmdLine.SaveMeta(fw);
      Logger.WriteLog("\nFinished", false);
      return 0;
    }

    private static bool InputValidation(string[] args)
    {
      if (!CmdLine.ParseArgs(args))
      {
        Logger.WriteLog("Error parsing arguments\n", false);
        return false;
      }
      if (!string.IsNullOrEmpty(CmdLine.file) && !File.Exists(CmdLine.file))
      {
        Logger.WriteLog("File " + CmdLine.file + " does not exist\n", false);
        return false;
      }
      if (!string.IsNullOrEmpty(CmdLine.file) && !CmdLine.ParseFileName())
      {
        Logger.WriteLog("Could not parse filename. Make sure the filename was not edited\n", false);
        return false;
      }
      if (string.IsNullOrEmpty(CmdLine.folder) || Directory.Exists(CmdLine.folder))
        return true;
      Logger.WriteLog("Folder " + CmdLine.folder + " does not exist\n", false);
      return false;
    }

    public static void SetProgress(int value)
    {
      if (CmdLine.progressBar.Line == -1)
        return;
      CmdLine.progressBar.oldLine = Console.CursorTop;
      int size = (int) ((double) (Console.BufferWidth - 2) * (double) value / 100.0);
      if (CmdLine.progressBar.Progress != size)
      {
        Console.CursorTop = CmdLine.progressBar.Line;
        Console.CursorLeft = 1;
        Logger.WriteLog(new string(Utility.GetCharArray(size, '=')), true);
        CmdLine.progressBar.Progress = size;
      }
      Console.CursorTop = CmdLine.progressBar.oldLine;
      Console.CursorLeft = 0;
    }

    private static void DisplayUsage()
    {
      Logger.WriteLog("Usage:\n", false);
      Logger.WriteLog("Update check:", false);
      Logger.WriteLog("     SamFirm.exe -c -model [device model] -region [region code]\n                [-version [pda/csc/phone/data]] [-binary]", false);
      Logger.WriteLog("\nDecrypting:", false);
      Logger.WriteLog("     SamFirm.exe -file [path-to-file.zip.enc2] -version [pda/csc/phone/data]", false);
      Logger.WriteLog("     SamFirm.exe -file [path-to-file.zip.enc4] -version [pda/csc/phone/data] -logicValue [logicValue]", false);
      Logger.WriteLog("\nDownloading:", false);
      Logger.WriteLog("     SamFirm.exe -model [device model] -region [region code]\n                [-version [pda/csc/phone/data]] [-folder [output folder]]\n                [-binary] [-autodecrypt]", false);
    }

    private static void SaveMeta(Command.Firmware fw)
    {
      if (fw.Version == null || string.IsNullOrEmpty(CmdLine.fwdest))
        return;
      if (!string.IsNullOrEmpty(Path.GetDirectoryName(CmdLine.metafile)) && !Directory.Exists(Path.GetDirectoryName(CmdLine.metafile)))
        Directory.CreateDirectory(Path.GetDirectoryName(CmdLine.metafile));
      using (TextWriter text = (TextWriter) File.CreateText(CmdLine.metafile))
      {
        text.WriteLine("[SamFirmData]");
        text.WriteLine("Model=" + fw.Model);
        text.WriteLine("Devicename=" + fw.DisplayName);
        text.WriteLine("Region=" + fw.Region);
        text.WriteLine("Version=" + fw.Version);
        text.WriteLine("OS=" + fw.OS);
        text.WriteLine("Filesize=" + (object) new FileInfo(CmdLine.fwdest).Length);
        text.WriteLine("Filename=" + CmdLine.fwdest);
        text.WriteLine("LastModified=" + fw.LastModified);
      }
    }

    private static void CreateProgressbar()
    {
      try
      {
        Console.CursorTop = Console.CursorTop;
        char[] spaceArray = Utility.GetSpaceArray(Console.BufferWidth);
        spaceArray[0] = '[';
        spaceArray[spaceArray.Length - 1] = ']';
        CmdLine.progressBar.Line = Console.CursorTop;
        Logger.WriteLog(new string(spaceArray), true);
      }
      catch (IOException ex)
      {
        CmdLine.progressBar.Line = -1;
      }
    }

    private static bool ParseArgs(string[] args)
    {
      if (args.Length < 4 || args.Length > 12)
      {
        Logger.WriteLog("Error: Not enough / too many arguments", false);
        return false;
      }
      int index1;
      for (int index2 = 0; index2 < args.Length; index2 = index1 + 1)
      {
        string[] strArray = args;
        int index3 = index2;
        index1 = index3 + 1;
        switch (strArray[index3])
        {
          case "-file":
            CmdLine.file = args[index1];
            break;
          case "-version":
            CmdLine.version = args[index1];
            break;
          case "-logicValue":
            CmdLine.logicValue = args[index1];
            break;
          case "-folder":
            CmdLine.folder = args[index1];
            break;
          case "-region":
            CmdLine.region = args[index1].ToUpper();
            break;
          case "-model":
            CmdLine.model = args[index1];
            break;
          case "-binary":
            --index1;
            CmdLine.binary = true;
            break;
          case "-autodecrypt":
            --index1;
            CmdLine.autodecrypt = true;
            break;
          case "-c":
            --index1;
            CmdLine.checkonly = true;
            break;
          case "-meta":
            CmdLine.metafile = args[index1];
            break;
          default:
            return false;
        }
      }
      return true;
    }

    private static bool ParseFileName()
    {
      if (CmdLine.file.EndsWith(".enc4"))
        return true;
      string[] strArray = CmdLine.file.Split('_');
      if (strArray.Length < 2)
        return false;
      CmdLine.model = strArray[0];
      if (strArray[1].Length != 3)
        return false;
      CmdLine.region = strArray[1];
      return true;
    }

    public struct ProgressBarInfo
    {
      public int Progress;
      public int Line;
      public int oldLine;
    }
  }
}
