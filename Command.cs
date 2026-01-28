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
      bool binaryNature)
    {
      int fetchAttempts = 0;
      Command.Firmware firmware = new Command.Firmware();
      foreach (Func<string, string, string> fwFetchFunc in FWFetch.FWFetchFuncs)
      {
        string info = fwFetchFunc(model, region);
        if (!string.IsNullOrEmpty(info))
        {
          fetchAttempts++;
          firmware = Command.UpdateCheck(model, region, imei, info, binaryNature, true);
          if (firmware.Version == null)
          {
            if (firmware.ConnectionError)
              break;
          }
          else
          {
            break;
          }
        }
      }
      if (firmware.Version == null)
      {
        Logger.WriteLog($"Could not fetch info for {model}/{region}. Please verify the input or use manual info", false);
      }
      firmware.FetchAttempts = fetchAttempts;
      return firmware;
    }

    public static Command.Firmware UpdateCheck(
      string model,
      string region,
      string imei,
      string info,
      bool binaryNature,
      bool autoFetch = false)
    {
      if (string.IsNullOrEmpty(info))
        return new Command.Firmware();
      string pda = Utility.InfoExtract(info, "pda");
      if (string.IsNullOrEmpty(pda))
        return new Command.Firmware();
      string csc = Utility.InfoExtract(info, "csc");
      string phone = Utility.InfoExtract(info, "phone");
      string data = Utility.InfoExtract(info, "data");
      return Command.UpdateCheck(model, region, imei, pda, csc, phone, data, binaryNature, autoFetch);
    }

    public static Command.Firmware UpdateCheck(
      string model,
      string region,
      string imei,
      string pda,
      string csc,
      string phone,
      string data,
      bool binaryNature,
      bool autoFetch = false)
    {
      Command.Firmware firmware = new Command.Firmware();
      Logger.WriteLog($"Checking firmware for {model}/{region}/{pda}/{csc}/{phone}/{data}", false);
      int nonce = Web.GenerateNonce();
      if (nonce != 200)
      {
        Logger.WriteLog($"Error: Could not generate Nonce. Status code {nonce}", false);
        firmware.ConnectionError = true;
        return firmware;
      }

      int htmlstatus = Web.DownloadBinaryInform(Xml.GetXmlBinaryInform(model, region, imei, pda, csc, phone, data, binaryNature), out string xmlresponse);
      int xmlstatus = Utility.GetXMLStatusCode(xmlresponse);
      if (htmlstatus != 200 || xmlstatus != 200)
      {
        Logger.WriteLog($"Error: Could not send BinaryInform. Status code {htmlstatus}/{xmlstatus}", false);
        Utility.CheckHTMLXMLStatus(htmlstatus, xmlstatus);
        return firmware;
      }

      firmware.Version = Xml.GetXMLValue(xmlresponse, "FUSBody/Results/LATEST_FW_VERSION/Data", null, null);
      firmware.Model = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_MODEL_NAME/Data", null, null);
      firmware.DisplayName = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_MODEL_DISPLAYNAME/Data", null, null);
      firmware.OS = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LATEST_OS_VERSION/Data", null, null);
      firmware.LastModified = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LAST_MODIFIED/Data", null, null);
      firmware.Filename = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_NAME/Data", null, null);
      firmware.Size = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_BYTE_SIZE/Data", null, null);
      string xmlValue = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_CRC/Data", null, null);
      if (!string.IsNullOrEmpty(xmlValue))
      {
        firmware.CRC = BitConverter.GetBytes(Convert.ToUInt32(xmlValue)).Reverse().ToArray();
      }
      firmware.Model_Type = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_MODEL_TYPE/Data", null, null);
      firmware.Path = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/MODEL_PATH/Data", null, null);
      firmware.Region = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/DEVICE_LOCAL_CODE/Data", null, null);
      firmware.BinaryNature = int.Parse(Xml.GetXMLValue(xmlresponse, "FUSBody/Put/BINARY_NATURE/Data", null, null));
      if (Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_OPTION_FACTORY/Data", null, null) == "1")
        firmware.LogicValueFactory = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_VALUE_FACTORY/Data", null, null);
      if (Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_OPTION_HOME/Data", null, null) == "1")
        firmware.LogicValueHome = Xml.GetXMLValue(xmlresponse, "FUSBody/Put/LOGIC_VALUE_HOME/Data", null, null);

      if (!autoFetch)
      {
        if ($"{pda}/{csc}/{phone}/{pda}" == firmware.Version)
          Logger.WriteLog("\nCurrent firmware is latest:", false);
        else
          Logger.WriteLog("\nNewer firmware available:", false);
      }
      Logger.WriteLog($"Model: {firmware.Model}", false);
      Logger.WriteLog($"Version: {firmware.Version}", false);
      Logger.WriteLog($"OS: {firmware.OS}", false);
      Logger.WriteLog($"Filename: {firmware.Filename}", false);
      Logger.WriteLog($"Size: {firmware.Size} bytes", false);
      if (firmware.BinaryNature == 1 && !string.IsNullOrEmpty(firmware.LogicValueFactory))
        Logger.WriteLog($"LogicValue: {firmware.LogicValueFactory}", false);
      else if (!string.IsNullOrEmpty(firmware.LogicValueHome))
        Logger.WriteLog($"LogicValue: {firmware.LogicValueHome}", false);
      Logger.WriteLog("\n", false);
      return firmware;
    }

    public static int Download(
      string path,
      string file,
      string version,
      string region,
      string model_type,
      string saveTo,
      string size,
      bool gui = true)
    {
      int nonce = Web.GenerateNonce();
      if (nonce != 200)
      {
        Logger.WriteLog($"Error: Could not generate Nonce. Status code {nonce}", false);
        return -1;
      }
      int htmlstatus = Web.DownloadBinaryInit(Xml.GetXmlBinaryInit(file, version, region, model_type), out string xmlresponse);
      int xmlstatus = Utility.GetXMLStatusCode(xmlresponse);
      if (htmlstatus == 200 && xmlstatus == 200)
        return Web.DownloadBinary(path, file, saveTo, size, gui);
      Logger.WriteLog($"Error: Could not send BinaryInform. Status code {htmlstatus}/{xmlstatus}", false);
      Utility.CheckHTMLXMLStatus(htmlstatus, xmlstatus);
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
