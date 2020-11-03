// Decompiled with JetBrains decompiler
// Type: SamFirm.Logger
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System;
using System.IO;

namespace SamFirm
{
  internal class Logger
  {
    public static bool nologging = false;
    public static Form1 form;

    private static string GetTimeDate()
    {
      string empty = string.Empty;
      return DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
    }

    private static void CleanLog()
    {
      if (Utility.run_by_cmd)
        return;
      if (Logger.form.log_textbox.InvokeRequired)
      {
        Logger.form.log_textbox.Invoke((Delegate) (() =>
        {
          if (Logger.form.log_textbox.Lines.Length <= 30)
            return;
          Logger.form.log_textbox.Text.Remove(0, Logger.form.log_textbox.GetFirstCharIndexFromLine(1));
        }));
      }
      else
      {
        if (Logger.form.log_textbox.Lines.Length <= 30)
          return;
        Logger.form.log_textbox.Text.Remove(0, Logger.form.log_textbox.GetFirstCharIndexFromLine(1));
      }
    }

    public static void WriteLog(string str, bool raw = false)
    {
      if (Logger.nologging)
        return;
      Logger.CleanLog();
      if (!raw)
        str += "\n";
      if (Utility.run_by_cmd)
        Console.Write(str);
      else if (Logger.form.log_textbox.InvokeRequired)
      {
        Logger.form.log_textbox.Invoke((Delegate) (() =>
        {
          Logger.form.log_textbox.AppendText(str);
          Logger.form.log_textbox.ScrollToCaret();
        }));
      }
      else
      {
        Logger.form.log_textbox.AppendText(str);
        Logger.form.log_textbox.ScrollToCaret();
      }
    }

    public static void SaveLog()
    {
      if (string.IsNullOrEmpty(Logger.form.log_textbox.Text))
        return;
      if (File.Exists("SamFirm.log") && new FileInfo("SamFirm.log").Length > 2097152L)
      {
        File.Delete("SamFirm.log.old");
        File.Move("SamFirm.log", "SamFirm.log.old");
      }
      using (TextWriter textWriter = (TextWriter) new StreamWriter((Stream) new FileStream("SamFirm.log", FileMode.Append)))
      {
        textWriter.WriteLine();
        textWriter.WriteLine(Logger.GetTimeDate());
        foreach (string line in Logger.form.log_textbox.Lines)
          textWriter.WriteLine(line);
      }
    }
  }
}
