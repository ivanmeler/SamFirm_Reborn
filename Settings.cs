using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Settings
  {


    public static T ReadSetting<T>(string element)
    {
      bool returnString = typeof(T) == typeof(string);
      bool returnList = typeof(T) == typeof(string[]);
      if (!returnString && !returnList)
      {
        throw new ArgumentException("Return value must be String or String[] !");
      }
      string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
      string SettingFile = AppLocation + "SamFirm.xml";
      try
      {
        if (!File.Exists(SettingFile))
          Settings.GenerateSettings();
        string value = XDocument.Load(SettingFile).Element((XName)"SamFirm")?.Element((XName)element)?.Value;
        if (returnString)
          return (T)(object)(value ?? string.Empty);
        return (T)(object)(value?.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0]);
      }
      catch (Exception ex)
      {
        Logger.WriteLog("Error reading config file: " + ex.Message, false);
        return returnString ? (T)(object)string.Empty : (T)(object)new string[0];
      }
    }

    public static void SetSetting(string element, string value)
    {
      try
      {
        string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
        string SettingFile = AppLocation + "SamFirm.xml";
        if (!File.Exists(SettingFile))
          Settings.GenerateSettings();
        XDocument xdocument = XDocument.Load(SettingFile);
        XElement xelement = xdocument.Element((XName)"SamFirm").Element((XName)element);
        if (xelement == null)
          xdocument.Element((XName)"SamFirm").Add((object)new XElement((XName)element, (object)value));
        else
          xelement.Value = value;
        xdocument.Save(SettingFile);
      }
      catch (Exception ex)
      {
        Logger.WriteLog($"Error writing {element} to config file: " + ex.Message, false);
      }
    }

    public static void SetSetting(string element, IEnumerable<string> values)
    {
      SetSetting(element, string.Join(" ", values));
    }

    private static void GenerateSettings()
    {
     string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
     string SettingFile = AppLocation + "SamFirm.xml";
     File.WriteAllText(SettingFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<SamFirm>\r\n    <SaveFileDialog></SaveFileDialog>\r\n    <AutoInfo>true</AutoInfo>\r\n\t<Region></Region>\r\n\t<Model></Model>\r\n\t<Models></Models>\r\n\t<PDAVer></PDAVer>\r\n\t<CSCVer></CSCVer>\r\n\t<PHONEVer></PHONEVer>\r\n    <BinaryNature></BinaryNature>\r\n    <CheckCRC></CheckCRC>\r\n    <AutoDecrypt></AutoDecrypt>\r\n</SamFirm>");
    }
  }
}
