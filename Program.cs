// Decompiled with JetBrains decompiler
// Type: SamFirm.Program
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SamFirm
{
  internal static class Program
  {
    [STAThread]
    private static int Main(string[] args)
    {
      if (args.Length == 0)
      {
        Imports.FreeConsole();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run((Form) new Form1());
      }
      else
      {
        Utility.run_by_cmd = true;
        Environment.Exit(CmdLine.Main(args));
      }
      return 0;
    }

    private static void SendEnterToParent()
    {
      Imports.EnumWindows((Imports.EnumWindowsProc) ((wnd, param) =>
      {
        uint lpdwProcessId = 0;
        int windowThreadProcessId = (int) Imports.GetWindowThreadProcessId(wnd, out lpdwProcessId);
        Process parentProcess = Imports.ParentProcessUtilities.GetParentProcess();
        if ((long) lpdwProcessId != (long) parentProcess.Id)
          return true;
        Imports.SendMessage(wnd, 258U, (IntPtr) 13, (IntPtr) 0);
        return false;
      }), IntPtr.Zero);
    }
  }
}
