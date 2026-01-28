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
      if (Logger.form.richTextBoxLog.InvokeRequired)
      {
        Logger.form.richTextBoxLog.Invoke((Delegate)((Action)(() =>
        {
          if (Logger.form.richTextBoxLog.Lines.Length <= 30)
            return;
          Logger.form.richTextBoxLog.Text.Remove(0, Logger.form.richTextBoxLog.GetFirstCharIndexFromLine(1));
        })));
      }
      else
      {
        if (Logger.form.richTextBoxLog.Lines.Length <= 30)
          return;
        Logger.form.richTextBoxLog.Text.Remove(0, Logger.form.richTextBoxLog.GetFirstCharIndexFromLine(1));
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
      else if (Logger.form.richTextBoxLog.InvokeRequired)
      {
        Logger.form.richTextBoxLog.Invoke((Delegate)((Action)(() =>
        {
          Logger.form.richTextBoxLog.AppendText(str);
          Logger.form.richTextBoxLog.ScrollToCaret();
        })));
      }
      else
      {
        Logger.form.richTextBoxLog.AppendText(str);
        Logger.form.richTextBoxLog.ScrollToCaret();
      }
    }

    public static void SaveLog()
    {
      string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
      string LogFile = AppLocation + "SamFirm.log";
      string OldLogFile = AppLocation + "SamFirm.log.old";

      try
      {
        if (string.IsNullOrEmpty(Logger.form.richTextBoxLog.Text))
          return;
        if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 2097152L)
        {
          File.Delete(OldLogFile);
          File.Move(LogFile, OldLogFile);
        }
        using (TextWriter textWriter = new StreamWriter(new FileStream(LogFile, FileMode.Append)))
        {
          textWriter.WriteLine();
          textWriter.WriteLine(Logger.GetTimeDate());
          foreach (string line in Logger.form.richTextBoxLog.Lines)
            textWriter.WriteLine(line);
        }
      }
      catch { }
    }
  }
}
