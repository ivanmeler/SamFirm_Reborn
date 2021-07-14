using System;
using System.IO;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Settings
  {
    

    public static string ReadSetting(string element)
    {
      string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
      string SettingFile = AppLocation + "SamFirm.xml";
      try
      {
        if (!File.Exists(SettingFile))
          Settings.GenerateSettings();
        return XDocument.Load(SettingFile).Element((XName) "SamFirm").Element((XName) element).Value;
      }
      catch (Exception ex)
      {
        Logger.WriteLog("Error reading config file: " + ex.Message, false);
        return string.Empty;
      }
    }

    public static void SetSetting(string element, string value)
    {
     string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
     string SettingFile = AppLocation + "SamFirm.xml";
     if (!File.Exists(SettingFile))
        Settings.GenerateSettings();
      XDocument xdocument = XDocument.Load(SettingFile);
      XElement xelement = xdocument.Element((XName) "SamFirm").Element((XName) element);
      if (xelement == null)
        xdocument.Element((XName) "SamFirm").Add((object) new XElement((XName) element, (object) value));
      else
        xelement.Value = value;
      xdocument.Save(SettingFile);
    }

    private static void GenerateSettings()
    {
     string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
     string SettingFile = AppLocation + "SamFirm.xml";
     File.WriteAllText(SettingFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<SamFirm>\r\n    <SaveFileDialog></SaveFileDialog>\r\n    <AutoInfo>true</AutoInfo>\r\n\t<Region></Region>\r\n\t<Model></Model>\r\n\t<PDAVer></PDAVer>\r\n\t<CSCVer></CSCVer>\r\n\t<PHONEVer></PHONEVer>\r\n    <BinaryNature></BinaryNature>\r\n    <CheckCRC></CheckCRC>\r\n    <AutoDecrypt></AutoDecrypt>\r\n</SamFirm>");
    }
  }
}
