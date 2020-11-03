// Decompiled with JetBrains decompiler
// Type: SamFirm.Settings
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System;
using System.IO;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Settings
  {
    private const string SettingFile = "SamFirm.xml";

    public static string ReadSetting(string element)
    {
      try
      {
        if (!File.Exists("SamFirm.xml"))
          Settings.GenerateSettings();
        return XDocument.Load("SamFirm.xml").Element((XName) "SamFirm").Element((XName) element).Value;
      }
      catch (Exception ex)
      {
        Logger.WriteLog("Error reading config file: " + ex.Message, false);
        return string.Empty;
      }
    }

    public static void SetSetting(string element, string value)
    {
      if (!File.Exists("SamFirm.xml"))
        Settings.GenerateSettings();
      XDocument xdocument = XDocument.Load("SamFirm.xml");
      XElement xelement = xdocument.Element((XName) "SamFirm").Element((XName) element);
      if (xelement == null)
        xdocument.Element((XName) "SamFirm").Add((object) new XElement((XName) element, (object) value));
      else
        xelement.Value = value;
      xdocument.Save("SamFirm.xml");
    }

    private static void GenerateSettings()
    {
      File.WriteAllText("SamFirm.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<SamFirm>\r\n    <SaveFileDialog></SaveFileDialog>\r\n    <AutoInfo></AutoInfo>\r\n\t<Region></Region>\r\n\t<Model></Model>\r\n\t<PDAVer></PDAVer>\r\n\t<CSCVer></CSCVer>\r\n\t<PHONEVer></PHONEVer>\r\n    <BinaryNature></BinaryNature>\r\n    <CheckCRC></CheckCRC>\r\n    <AutoDecrypt></AutoDecrypt>\r\n</SamFirm>");
    }
  }
}
