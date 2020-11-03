using DamienG.Security.Cryptography;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace SamFirm
{
  public static class Utility
  {
    public static bool run_by_cmd = false;
    public static bool ReconnectDownload = false;
    private static Stopwatch dswatch = new Stopwatch();
    private static int interval = 0;
    private static long lastBread = 0;
    private static int lastSpeed = 0;

    public static bool IsRunningOnMono()
    {
      return Type.GetType("Mono.Runtime") != null;
    }

    public static void Reconnect(Action<object, EventArgs> action)
    {
      BackgroundWorker backgroundWorker = new BackgroundWorker();
      backgroundWorker.DoWork += (DoWorkEventHandler) ((o, _e) =>
      {
        Thread.Sleep(1000);
        if (!Utility.CheckConnection("cloud-neofussvr.sslcs.cdngc.net", ref Utility.ReconnectDownload))
          return;
        action((object) null, (EventArgs) new Form1.DownloadEventArgs()
        {
          isReconnect = true
        });
      });
      backgroundWorker.RunWorkerAsync();
    }

    public static void ReconnectCmdLine()
    {
      Utility.CheckConnection("cloud-neofussvr.sslcs.cdngc.net", ref Utility.ReconnectDownload);
    }

    public static bool CheckConnection(string address, ref bool docheck)
    {
      bool flag = false;
      Ping ping = new Ping();
      while (docheck)
      {
        if (!flag)
        {
          try
          {
            flag = ping.Send(address, 2000).Status == IPStatus.Success;
          }
          catch (PingException ex)
          {
          }
        }
        else
          goto label_6;
      }
      flag = false;
label_6:
      return flag;
    }

    public static void PreventDeepSleep(Utility.PDSMode mode)
    {
      switch (mode)
      {
        case Utility.PDSMode.Start:
          Utility.dswatch.Reset();
          Utility.dswatch.Start();
          break;
        case Utility.PDSMode.Stop:
          Utility.dswatch.Stop();
          break;
      }
      if (Utility.dswatch.ElapsedMilliseconds <= 30000L)
        return;
      int num = (int) Imports.SetThreadExecutionState(Imports.EXECUTION_STATE.ES_SYSTEM_REQUIRED);
      Utility.PreventDeepSleep(Utility.PDSMode.Start);
    }

    public static bool CRCCheck(string file, byte[] crc)
    {
      if (!System.IO.File.Exists(file))
        throw new FileNotFoundException("File for crc check not found");
      byte[] hash;
      using (FileStream fileStream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read))
        hash = new Crc32().ComputeHash((Stream) fileStream);
      return crc.Compare(hash);
    }

    public static bool Compare(this byte[] arr1, byte[] arr2)
    {
      if (arr1.Length != arr2.Length)
        return false;
      for (int index = 0; index < arr1.Length; ++index)
      {
        if ((int) arr1[index] != (int) arr2[index])
          return false;
      }
      return true;
    }

    public static void ResetSpeed(long _lastBread)
    {
      Utility.interval = Utility.lastSpeed = 0;
      Utility.lastBread = _lastBread;
    }

    public static int DownloadSpeed(long bread, Stopwatch sw)
    {
      if (!sw.IsRunning)
        sw.Start();
      if (Utility.interval < 150)
      {
        ++Utility.interval;
        return -1;
      }
      Utility.interval = 0;
      double num1 = (double) sw.ElapsedMilliseconds / 1000.0;
      int num2 = (int) Math.Floor((double) (bread - Utility.lastBread) / num1 / 1024.0);
      if (Utility.lastSpeed != 0)
        num2 = (Utility.lastSpeed + num2) / 2;
      Utility.lastSpeed = num2;
      Utility.lastBread = bread;
      sw.Reset();
      return Utility.Round(num2, 2);
    }

    public static int Round(int num, int pos)
    {
      double num1 = Math.Pow(10.0, (double) pos);
      if (num1 > (double) num)
        return num;
      return num / (int) num1 * (int) num1;
    }

    public static char[] GetSpaceArray(int size)
    {
      return Utility.GetCharArray(size, ' ');
    }

    public static char[] GetCharArray(int size, char init)
    {
      char[] chArray = new char[size];
      for (int index = 0; index < size; ++index)
        chArray[index] = init;
      return chArray;
    }

    public static int GetProgress(long value, long total)
    {
      return (int) (float) ((double) value / (double) total * 100.0);
    }

    public static int GetXMLStatusCode(string xml)
    {
      if (string.IsNullOrEmpty(xml))
        return 0;
      int result;
      if (int.TryParse(Xml.GetXMLValue(xml, "FUSBody/Results/Status", (string) null, (string) null), out result))
        return result;
      return 666;
    }

    public static string GetLogicCheck(string input, string nonce)
    {
      if (string.IsNullOrEmpty(input))
        return string.Empty;
      StringBuilder stringBuilder = new StringBuilder();
      int num1 = 0;
      if (input.EndsWith(".zip.enc2") || input.EndsWith(".zip.enc4"))
        num1 = input.Length - 25;
      foreach (int num2 in nonce)
      {
        int num3 = num2 & 15;
        if (input.Length <= num3 + num1)
          return string.Empty;
        stringBuilder.Append(input[num3 + num1]);
      }
      return stringBuilder.ToString();
    }

    public static int CheckHTMLXMLStatus(int htmlstatus, int xmlstatus = 0)
    {
      int num = xmlstatus == 0 ? htmlstatus : xmlstatus;
      switch (num)
      {
        case 400:
          Logger.WriteLog("    Request was invalid. Are you sure the input data is correct?", false);
          break;
        case 401:
          Logger.WriteLog("    Authorization failed", false);
          break;
      }
      return num;
    }

    public static string InfoExtract(string info, string type)
    {
      string[] strArray = info.Split('/');
      if (strArray.Length < 2)
        return string.Empty;
      switch (type)
      {
        case "pda":
          return strArray[0];
        case "csc":
          return strArray[1];
        case "phone":
          if (strArray.Length < 3 || string.IsNullOrEmpty(strArray[2]))
            return strArray[0];
          return strArray[2];
        case "data":
          if (strArray.Length < 4)
            return strArray[0];
          return strArray[3];
        default:
          return string.Empty;
      }
    }

    public static string GetHtml(string url)
    {
      int num = 0;
      while (true)
      {
        try
        {
          using (WebClient webClient = new WebClient())
            return webClient.DownloadString(url);
        }
        catch (WebException ex)
        {
          if (num < 2)
            ++num;
          else
            break;
        }
      }
      return string.Empty;
    }

    public static void TaskBarProgressState(bool paused)
    {
      try
      {
        if (paused)
          TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Paused);
        else
          TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
      }
      catch (Exception ex)
      {
      }
    }

    public enum PDSMode
    {
      Start,
      Stop,
      Continue,
    }
  }
}
