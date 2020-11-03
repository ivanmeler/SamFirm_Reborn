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
