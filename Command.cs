using System;
using System.Collections.Generic;
using System.Linq;

namespace SamFirm
{
  internal class Command
  {
    public static Command.Firmware UpdateCheckAuto(
      string model,
      string region,
      string imei,
      bool BinaryNature)
    {
      int num = 0;
      Command.Firmware firmware = new Command.Firmware();
      foreach (Func<string, string, string> fwFetchFunc in FWFetch.FWFetchFuncs)
      {
        string info = fwFetchFunc(model, region);
        if (!string.IsNullOrEmpty(info))
        {
          ++num;
          firmware = Command.UpdateCheck(model, region, imei, info, BinaryNature, true);
          if (firmware.Version == null)
          {
            if (firmware.ConnectionError)
              break;
          }
          else
            break;
        }
      }
      if (firmware.Version == null)
        Logger.WriteLog("Could not fetch info for " + model + "/" + region + ". Please verify the input or use manual info", false);
      firmware.FetchAttempts = num;
      return firmware;
    }

    public static Command.Firmware UpdateCheck(
      string model,
      string region,
      string imei,
      string info,
      bool BinaryNature,
      bool AutoFetch = false)
    {
      if (string.IsNullOrEmpty(info))
        return new Command.Firmware();
      string pda = Utility.InfoExtract(info, "pda");
      if (string.IsNullOrEmpty(pda))
        return new Command.Firmware();
      string csc = Utility.InfoExtract(info, "csc");
      string phone = Utility.InfoExtract(info, "phone");
      string data = Utility.InfoExtract(info, "data");
      return Command.UpdateCheck(model, region, imei, pda, csc, phone, data, BinaryNature, AutoFetch);
    }

    public static Command.Firmware UpdateCheck(
      string model,
      string region,
      string imei,
      string pda,
      string csc,
      string phone,
      string data,
      bool BinaryNature,
      bool AutoFetch = false)
    {
      Command.Firmware firmware = new Command.Firmware();
      Logger.WriteLog("Checking firmware for " + model + "/" + region + "/" + pda + "/" + csc + "/" + phone + "/" + data, false);
      int nonce = Web.GenerateNonce();
      if (nonce != 200)
      {
        Logger.WriteLog("Error: Could not generate Nonce. Status code " + (object) nonce, false);
        firmware.ConnectionError = true;
        return firmware;
      }
      //Logger.WriteLog($"Encrypted Nonce; {Web.EncryptedNonce}", false);
      //Logger.WriteLog($"Nonce; {Web.Nonce}", false);
      ////Logger.WriteLog($"Auth signature: {Imports.GetAuthorization(Web.Nonce)}");
      //Logger.WriteLog($"Auth signature: {Web.AuthHeaderNoNonce}");

      string xmlresponse;
      int htmlstatus = Web.DownloadBinaryInform(Xml.GetXmlBinaryInform(model, region, imei, pda, csc, phone, data, BinaryNature), out xmlresponse);
      if (htmlstatus != 200 || Utility.GetXMLStatusCode(xmlresponse) != 200)
      {
        Logger.WriteLog("Error: Could not send BinaryInform. Status code " + (object) htmlstatus + "/" + (object) Utility.GetXMLStatusCode(xmlresponse), false);
        Utility.CheckHTMLXMLStatus(htmlstatus, Utility.GetXMLStatusCode(xmlresponse));
        return firmware;
      }
      List<KeyValuePair<string, string>> responseData = Xml.GetXMLDataValues(xmlresponse);
      firmware.Version = GetFirstXmlValue(xmlresponse, "FUSBody/Results/BINARY_SW_VERSION/Data", "FUSBody/Results/LATEST_FW_VERSION/Data", "FUSBody/Put/BINARY_SW_VERSION/Data");
      firmware.Model = GetFirstXmlValue(xmlresponse, "FUSBody/Put/BINARY_MODEL_NAME/Data", "FUSBody/Put/DEVICE_MODEL_NAME/Data");
      firmware.DisplayName = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_MODEL_DISPLAYNAME/Data", (string) null, (string) null);
      firmware.OS = GetFirstDataValue(responseData, "LATEST_OS_VERSION", "BINARY_OS_VERSION", "OS_VERSION", "ANDROID_VERSION", "ANDROID_OS_VERSION", "DEVICE_OS_VERSION", "DEVICE_PLATFORM_VERSION");
      if (string.IsNullOrEmpty(firmware.OS))
        firmware.OS = GetFirstDataValueContaining(responseData, "OS", "ANDROID");
      firmware.LastModified = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LAST_MODIFIED/Data", (string) null, (string) null);
      firmware.Filename = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_NAME/Data", (string) null, (string) null);
      firmware.Size = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_BYTE_SIZE/Data", (string) null, (string) null);
      string xmlValue = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_CRC/Data", (string) null, (string) null);
      if (!string.IsNullOrEmpty(xmlValue))
        firmware.CRC = ((IEnumerable<byte>) BitConverter.GetBytes(Convert.ToUInt32(xmlValue))).Reverse<byte>().ToArray<byte>();
      firmware.Model_Type = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_MODEL_TYPE/Data", (string) null, (string) null);
      firmware.Path = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/MODEL_PATH/Data", (string) null, (string) null);
      firmware.Region = GetFirstXmlValue(xmlresponse, "FUSBody/Put/BINARY_LOCAL_CODE/Data", "FUSBody/Put/DEVICE_LOCAL_CODE/Data");
      int binaryNature;
      firmware.BinaryNature = int.TryParse(Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_NATURE/Data", (string) null, (string) null), out binaryNature) ? binaryNature : 1;
      string logicValueFactory = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_VALUE_FACTORY/Data", (string) null, (string) null);
      string logicValueHome = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_VALUE_HOME/Data", (string) null, (string) null);
      if (Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_OPTION_FACTORY/Data", (string) null, (string) null) == "1" || !string.IsNullOrEmpty(logicValueFactory))
        firmware.LogicValueFactory = logicValueFactory;
      if (Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_OPTION_HOME/Data", (string) null, (string) null) == "1" || !string.IsNullOrEmpty(logicValueHome))
        firmware.LogicValueHome = logicValueHome;
      if (!AutoFetch)
      {
        if (pda + "/" + csc + "/" + phone + "/" + pda == firmware.Version)
          Logger.WriteLog("\nCurrent firmware is latest:", false);
        else
          Logger.WriteLog("\nNewer firmware available:", false);
      }
      Logger.WriteLog("Model: " + firmware.Model, false);
      Logger.WriteLog("Version: " + firmware.Version, false);
      Logger.WriteLog("OS: " + firmware.OS, false);
      Logger.WriteLog("Filename: " + firmware.Filename, false);
      Logger.WriteLog("Size: " + firmware.Size + " bytes", false);
      if (firmware.BinaryNature == 1 && !string.IsNullOrEmpty(firmware.LogicValueFactory))
        Logger.WriteLog("LogicValue: " + firmware.LogicValueFactory, false);
      else if (!string.IsNullOrEmpty(firmware.LogicValueHome))
        Logger.WriteLog("LogicValue: " + firmware.LogicValueHome, false);
      LogFirmwareResponseData(responseData);
      Logger.WriteLog("\n", false);
      return firmware;
    }

    private static string GetFirstXmlValue(string xml, params string[] paths)
    {
      foreach (string path in paths)
      {
        string value = Xml.GetXMLValue(xml, path, (string)null, (string)null);
        if (!string.IsNullOrEmpty(value))
          return value;
      }
      return string.Empty;
    }

    private static string GetFirstDataValue(List<KeyValuePair<string, string>> values, params string[] names)
    {
      foreach (string name in names)
      {
        foreach (KeyValuePair<string, string> value in values)
        {
          if (string.Equals(value.Key, name, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value.Value))
            return value.Value;
        }
      }
      return string.Empty;
    }

    private static string GetFirstDataValueContaining(List<KeyValuePair<string, string>> values, params string[] fragments)
    {
      foreach (KeyValuePair<string, string> value in values)
      {
        foreach (string fragment in fragments)
        {
          if (value.Key.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrEmpty(value.Value))
            return value.Value;
        }
      }
      return string.Empty;
    }

    private static void LogFirmwareResponseData(List<KeyValuePair<string, string>> values)
    {
      if (values.Count == 0)
        return;

      Logger.WriteLog("Firmware response data:", false);
      HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (KeyValuePair<string, string> value in values)
      {
        string key = value.Key;
        string data = value.Value;
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(data))
          continue;

        string unique = key + "\n" + data;
        if (!seen.Add(unique))
          continue;

        Logger.WriteLog("  " + FormatDataName(key) + ": " + data, false);
      }
    }

    private static string FormatDataName(string name)
    {
      switch (name)
      {
        case "DEVICE_BOOT_FILE":
          return "DEVICE BOOTLOADER FILE";
        case "DEVICE_PDA_CODE1_FILE":
          return "DEVICE SYSTEM FILE";
        case "DEVICE_PHONE_FONT_FILE":
          return "DEVICE MODEM FILE";
        case "DEVICE_CSC_FILE":
          return "DEVICE CSC FILE";
      }
      return name.Replace('_', ' ');
    }

    public static int Download(
      string path,
      string file,
      string version,
      string region,
      string model_type,
      string saveTo,
      string size,
      bool GUI = true)
    {
      int nonce = Web.GenerateNonce();
      if (nonce != 200)
      {
        Logger.WriteLog("Error: Could not generate Nonce. Status code " + (object) nonce, false);
        return -1;
      }
      string xmlresponse;
      int htmlstatus = Web.DownloadBinaryInit(Xml.GetXmlBinaryInit(file, version, region, model_type), out xmlresponse);
      if (htmlstatus == 200 && Utility.GetXMLStatusCode(xmlresponse) == 200)
        return Web.DownloadBinary(path, file, saveTo, size, GUI);
      Logger.WriteLog("Error: Could not send BinaryInform. Status code " + (object) htmlstatus + "/" + (object) Utility.GetXMLStatusCode(xmlresponse), false);
      Utility.CheckHTMLXMLStatus(htmlstatus, Utility.GetXMLStatusCode(xmlresponse));
      return -1;
    }

    public struct Firmware
    {
      public string Model;
      public string DisplayName;
      public string Version;
      public string OS;
      public string LastModified;
      public string Filename;
      public string Path;
      public string Size;
      public byte[] CRC;
      public string Model_Type;
      public string Region;
      public int BinaryNature;
      public string LogicValueFactory;
      public string LogicValueHome;
      public string Announce;
      public bool ConnectionError;
      public int FetchAttempts;
    }
  }
}
