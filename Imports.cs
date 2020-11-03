using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SamFirm
{
  internal class Imports
  {
    private static IntPtr mod = IntPtr.Zero;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern Imports.EXECUTION_STATE SetThreadExecutionState(
      Imports.EXECUTION_STATE esFlags);

    [DllImport("kernel32.dll")]
    public static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool FreeConsole();

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(Imports.EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(
      IntPtr hWnd,
      uint Msg,
      IntPtr wParam,
      IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    private static int LoadModule(string module = "AgentModule.dll")
    {
      try
      {
        if (!File.Exists(module))
        {
          string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
          if (File.Exists(Path.Combine(directoryName, module)))
          {
            module = Path.Combine(directoryName, module);
          }
          else
          {
            Logger.WriteLog("Error: Library " + module + " does not exist", false);
            return 1;
          }
        }
        Imports.mod = Imports.LoadLibrary(module);
        if (Imports.mod == IntPtr.Zero)
        {
          Logger.WriteLog("Error loading library: " + (object) Marshal.GetLastWin32Error(), false);
          Logger.WriteLog("Please make sure \"Microsoft Visual C++ 2008 Redistributable Package (x86)\" and \"Microsoft Visual C++ 2010 Redistributable Package (x86)\" are installed", false);
          return 1;
        }
      }
      catch (Exception ex)
      {
        Logger.WriteLog("Error LoadModule: " + ex.Message, false);
        return 1;
      }
      return 0;
    }

    public static void FreeModule()
    {
      if (Imports.mod == IntPtr.Zero)
        return;
      if (!Imports.FreeLibrary(Imports.mod))
        Logger.WriteLog("Error: Unable to free library", false);
      Imports.mod = IntPtr.Zero;
    }

    private static T load_function<T>(IntPtr module, string name) where T : class
    {
      return Marshal.GetDelegateForFunctionPointer(Imports.GetProcAddress(module, name), typeof (T)) as T;
    }

    public static string GetAuthorization(string Nonce)
    {
      if (Imports.mod == IntPtr.Zero && Imports.LoadModule("AgentModule.dll") != 0)
        return string.Empty;
      Imports.Auth_t authT = Imports.load_function<Imports.Auth_t>(Imports.mod, "?MakeAuthorizationHeaderWithGeneratedNonceValueAndAMModule@AgentNetworkModule@@CAPB_WPB_W@Z");
      IntPtr hglobalUni = Marshal.StringToHGlobalUni(Nonce);
      string stringUni = Marshal.PtrToStringUni(authT(hglobalUni));
      Marshal.FreeHGlobal(hglobalUni);
      return stringUni;
    }

    public enum EXECUTION_STATE : uint
    {
      ES_SYSTEM_REQUIRED = 1,
      ES_DISPLAY_REQUIRED = 2,
      ES_AWAYMODE_REQUIRED = 64, // 0x00000040
      ES_CONTINUOUS = 2147483648, // 0x80000000
    }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr Auth_t(IntPtr nonce);

    public struct ParentProcessUtilities
    {
      internal IntPtr Reserved1;
      internal IntPtr PebBaseAddress;
      internal IntPtr Reserved2_0;
      internal IntPtr Reserved2_1;
      internal IntPtr UniqueProcessId;
      internal IntPtr InheritedFromUniqueProcessId;

      [DllImport("ntdll.dll")]
      private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref Imports.ParentProcessUtilities processInformation,
        int processInformationLength,
        out int returnLength);

      public static Process GetParentProcess()
      {
        return Imports.ParentProcessUtilities.GetParentProcess(Process.GetCurrentProcess().Handle);
      }

      public static Process GetParentProcess(int id)
      {
        return Imports.ParentProcessUtilities.GetParentProcess(Process.GetProcessById(id).Handle);
      }

      public static Process GetParentProcess(IntPtr handle)
      {
        Imports.ParentProcessUtilities processInformation = new Imports.ParentProcessUtilities();
        int returnLength;
        int error = Imports.ParentProcessUtilities.NtQueryInformationProcess(handle, 0, ref processInformation, Marshal.SizeOf((object) processInformation), out returnLength);
        if (error != 0)
          throw new Win32Exception(error);
        try
        {
          return Process.GetProcessById(processInformation.InheritedFromUniqueProcessId.ToInt32());
        }
        catch (ArgumentException ex)
        {
          return (Process) null;
        }
      }
    }
  }
}
