using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SamFirm
{
  internal class FWFetch
  {
    public static List<Func<string, string, string>> FWFetchFuncs = new List<Func<string, string, string>>()
    {
      new Func<string, string, string>(FWFetch.FOTAInfoFetch1),
      new Func<string, string, string>(FWFetch.FOTAInfoFetch2),
      new Func<string, string, string>(FWFetch.SamsungFirmwareOrgFetch1),
      new Func<string, string, string>(FWFetch.SamsungFirmwareOrgFetch2),
      new Func<string, string, string>(FWFetch.SamMobileFetch1),
      new Func<string, string, string>(FWFetch.SamMobileFetch2),
      new Func<string, string, string>(FWFetch.SamsungUpdatesFetch)
    };
    private static string SamsungFirmwareOrgHtml;
    private static string SamMobileHtml;

    public static string FOTAInfoFetch1(string model, string region)
    {
      return FWFetch.FOTAInfoFetch(model, region, true);
    }

    public static string FOTAInfoFetch2(string model, string region)
    {
      return FWFetch.FOTAInfoFetch(model, region, false);
    }

    public static string SamMobileFetch1(string model, string region)
    {
      try
      {
        FWFetch.SamMobileHtml = Utility.GetHtml("http://www.sammobile.com/firmwares/database/" + model + "/" + region + "/");
        return FWFetch.SamMobileFetch(model, region, 0);
      }
      catch (Exception ex)
      {
        return string.Empty;
      }
    }

    public static string SamMobileFetch2(string model, string region)
    {
      return FWFetch.SamMobileFetch(model, region, 1);
    }

    public static string SamsungFirmwareOrgFetch1(string model, string region)
    {
      FWFetch.SamsungFirmwareOrgHtml = Utility.GetHtml("https://samsung-firmware.org/model/" + model + "/region/" + region + "/");
      return FWFetch.SamsungFirmwareOrgFetch(model, region);
    }

    public static string SamsungFirmwareOrgFetch2(string model, string region)
    {
      string str = FWFetch.SamsungFirmwareOrgFetch(model, region);
      if (!string.IsNullOrEmpty(str))
      {
        string[] strArray = str.Split('/');
        str = strArray[0] + "/" + strArray[2] + "/" + strArray[1];
      }
      return str;
    }

    public static string FOTAInfoFetch(string model, string region, bool latest = true)
    {
      try
      {
        using (WebClient webClient = new WebClient())
        {
          string xml = webClient.DownloadString("http://fota-cloud-dn.ospserver.net/firmware/" + region + "/" + model + "/version.xml");
          return !latest ? Xml.GetXMLValue(xml, "firmware/version/upgrade/value", (string) null, (string) null).ToUpper() : Xml.GetXMLValue(xml, "firmware/version/latest", (string) null, (string) null).ToUpper();
        }
      }
      catch (Exception ex)
      {
        return string.Empty;
      }
    }

    public static string SamsungFirmwareOrgFetch(string model, string region)
    {
      string samsungFirmwareOrgHtml = FWFetch.SamsungFirmwareOrgHtml;
      int startIndex = samsungFirmwareOrgHtml.IndexOf("\"/model/" + model + "/\"");
      if (startIndex < 0)
        return string.Empty;
      string s = samsungFirmwareOrgHtml.Substring(startIndex);
      string url = "https://samsung-firmware.org";
      using (StringReader stringReader = new StringReader(s))
      {
        string str;
        while ((str = stringReader.ReadLine()) != null)
        {
          if (str.Contains("Download"))
          {
            int num = str.IndexOf('"');
            int length = str.Substring(num + 1).IndexOf('"');
            url += str.Substring(num + 1, length);
            break;
          }
        }
      }
      string html = Utility.GetHtml(url);
      string infoSfo1 = FWFetch.GetInfoSFO(html, "PDA Version");
      string infoSfo2 = FWFetch.GetInfoSFO(html, "CSC Version");
      string infoSfo3 = FWFetch.GetInfoSFO(html, "PHONE Version");
      if (string.IsNullOrEmpty(infoSfo1) || string.IsNullOrEmpty(infoSfo2) || string.IsNullOrEmpty(infoSfo3))
        return string.Empty;
      return infoSfo1 + "/" + infoSfo2 + "/" + infoSfo3;
    }

    private static string GetInfoSFO(string html, string search)
    {
      if (string.IsNullOrEmpty(html))
        return string.Empty;
      int num = html.IndexOf(">" + search + "<");
      if (num < 0)
        return string.Empty;
      int startIndex = num + (search.Length + 1 + 19);
      string str = html.Substring(startIndex);
      return str.Substring(0, str.IndexOf('<'));
    }

    public static string SamMobileFetch(string model, string region, int index)
    {
      string samMobileHtml = FWFetch.SamMobileHtml;
      if (string.IsNullOrEmpty(samMobileHtml) || samMobileHtml.Contains("Device model not found"))
        return string.Empty;
      int num = 0;
      while (index-- >= 0 && num >= 0)
        num = samMobileHtml.IndexOf("<a class=\"firmware-table-row\" href=\"", num + 1);
      if (num < 0)
        return string.Empty;
      string str1 = samMobileHtml.Substring(num + 36);
      string html = Utility.GetHtml(str1.Substring(0, str1.IndexOf('"')));
      string input = string.Empty;
      using (StringReader stringReader = new StringReader(html))
      {
        bool flag = false;
        string line;
        while ((line = stringReader.ReadLine()) != null)
        {
          string str2 = FWFetch.tdExtract(line).Trim();
          if (str2 == "PDA" || str2 == "CSC")
            flag = true;
          else if (flag)
          {
            input = input + str2 + "/";
            flag = false;
          }
        }
      }
      return Regex.Replace(input, "^(.*)/(.*)/$", "$1/$2/$1");
    }

    private static string tdExtract(string line)
    {
      int startIndex = line.IndexOf("<td>") + 4;
      int num = line.IndexOf("</td>");
      if (startIndex < 0 || num < 0)
        return string.Empty;
      return line.Substring(startIndex, num - startIndex);
    }

    private static string SamsungUpdatesFetch(string model, string region)
    {
      try
      {
        using (WebClient webClient = new WebClient())
        {
          string s = webClient.DownloadString("http://samsung-updates.com/device/?id=" + model);
          string address = "http://samsung-updates.com";
          bool flag = false;
          using (StringReader stringReader = new StringReader(s))
          {
            string str;
            while ((str = stringReader.ReadLine()) != null)
            {
              if (str.Contains("/" + model + "/" + region + "/"))
              {
                int num = str.IndexOf("a href=\"");
                int length = str.Substring(num + 8).IndexOf('"');
                address += str.Substring(num + 8, length);
                flag = true;
                break;
              }
            }
          }
          if (!flag)
            return string.Empty;
          string input = webClient.DownloadString(address);
          Match match1 = Regex.Match(input, "PDA:</b> ([^\\s]+) <b>");
          if (!match1.Success)
            return string.Empty;
          string format = match1.Groups[1].Value + "/{0}/" + match1.Groups[1].Value;
          Match match2 = Regex.Match(input, "CSC:</b> ([^\\s]+) <b>");
          if (!match2.Success)
            return string.Empty;
          return string.Format(format, (object) match2.Groups[1].Value);
        }
      }
      catch (WebException ex)
      {
        return string.Empty;
      }
    }
  }
}
