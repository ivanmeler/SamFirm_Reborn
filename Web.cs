// Decompiled with JetBrains decompiler
// Type: SamFirm.Web
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SamFirm
{
  internal class Web
  {
    public static string JSessionID = string.Empty;
    public static string Nonce = string.Empty;
    public static Form1 form;

    public static int GenerateNonce()
    {
      HttpWebRequest wr = KiesRequest.Create("https://neofussvr.sslcs.cdngc.net/NF_DownloadGenerateNonce.do");
      wr.Method = "POST";
      wr.ContentLength = 0L;
      using (HttpWebResponse responseFus = (HttpWebResponse) wr.GetResponseFUS())
      {
        if (responseFus == null)
          return 901;
        int statusCode = (int) responseFus.StatusCode;
        return (int) responseFus.StatusCode;
      }
    }

    public static int DownloadBinaryInform(string xml, out string xmlresponse)
    {
      return Web.XMLFUSRequest("https://neofussvr.sslcs.cdngc.net/NF_DownloadBinaryInform.do", xml, out xmlresponse);
    }

    public static int DownloadBinaryInit(string xml, out string xmlresponse)
    {
      return Web.XMLFUSRequest("https://neofussvr.sslcs.cdngc.net/NF_DownloadBinaryInitForMass.do", xml, out xmlresponse);
    }

    private static int XMLFUSRequest(string URL, string xml, out string xmlresponse)
    {
      xmlresponse = (string) null;
      HttpWebRequest wr = KiesRequest.Create(URL);
      wr.Method = "POST";
      wr.Headers["Authorization"] = "FUS nonce=\"\", signature=\"" + Imports.GetAuthorization(Web.Nonce) + "\", nc=\"\", type=\"\", realm=\"\"";
      byte[] bytes = Encoding.ASCII.GetBytes(Regex.Replace(xml, "\\r\\n?|\\n|\\t", string.Empty));
      wr.ContentLength = (long) bytes.Length;
      using (Stream requestStream = wr.GetRequestStream())
        requestStream.Write(bytes, 0, bytes.Length);
      using (HttpWebResponse responseFus = (HttpWebResponse) wr.GetResponseFUS())
      {
        if (responseFus == null)
          return 901;
        if (responseFus.StatusCode == HttpStatusCode.OK)
        {
          try
          {
            xmlresponse = new StreamReader(responseFus.GetResponseStream()).ReadToEnd();
          }
          catch (Exception ex)
          {
            return 900;
          }
        }
        return (int) responseFus.StatusCode;
      }
    }

    public static void SetReconnect()
    {
      if (!Utility.run_by_cmd)
      {
        if (!Web.form.PauseDownload)
          Utility.ReconnectDownload = true;
        Web.form.PauseDownload = true;
      }
      else
        Utility.ReconnectDownload = true;
    }

    public static int DownloadBinary(
      string path,
      string file,
      string saveTo,
      string size,
      bool GUI = true)
    {
      long num = 0;
      HttpWebRequest wr = KiesRequest.Create("http://cloud-neofussvr.sslcs.cdngc.net/NF_DownloadBinaryForMass.do?file=" + path + file);
      wr.Method = "GET";
      wr.Headers["Authorization"] = Imports.GetAuthorization(Web.Nonce).Replace("Authorization: ", "").Replace("nonce=\"", "nonce=\"" + Web.Nonce);
      wr.Timeout = 25000;
      wr.ReadWriteTimeout = 25000;
      if (System.IO.File.Exists(saveTo))
      {
        long length = new FileInfo(saveTo).Length;
        if (long.Parse(size) == length)
        {
          Logger.WriteLog("File already downloaded.", false);
          return 200;
        }
        Logger.WriteLog("File exists. Resuming download...", false);
        wr.AddRange((int) length);
        num = length;
      }
      using (HttpWebResponse responseFus = (HttpWebResponse) wr.GetResponseFUS())
      {
        if (responseFus == null)
        {
          Logger.WriteLog("Error downloading: response is null", false);
          return 901;
        }
        if (responseFus.StatusCode != HttpStatusCode.OK && responseFus.StatusCode != HttpStatusCode.PartialContent)
        {
          Logger.WriteLog("Error downloading: " + (object) (int) responseFus.StatusCode, false);
        }
        else
        {
          long total = long.Parse(responseFus.GetResponseHeader("content-length")) + num;
          if (!System.IO.File.Exists(saveTo) || new FileInfo(saveTo).Length != total)
          {
            byte[] buffer = new byte[8192];
            Stopwatch sw = new Stopwatch();
            Utility.ResetSpeed(num);
            try
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Start);
              using (BinaryWriter binaryWriter = new BinaryWriter((Stream) new FileStream(saveTo, FileMode.Append)))
              {
                int count;
                do
                {
                  Utility.PreventDeepSleep(Utility.PDSMode.Continue);
                  if (GUI)
                  {
                    if (Web.form.PauseDownload)
                      break;
                  }
                  num += (long) (count = responseFus.GetResponseStream().Read(buffer, 0, buffer.Length));
                  if (count > 0)
                  {
                    binaryWriter.Write(buffer, 0, count);
                    if (GUI)
                    {
                      int dlspeed = Utility.DownloadSpeed(num, sw);
                      if (dlspeed != -1)
                        Web.form.lbl_speed.Invoke((Delegate) (() => Web.form.lbl_speed.Text = dlspeed.ToString() + "kB/s"));
                    }
                  }
                  if (GUI)
                    Web.form.SetProgressBar(Utility.GetProgress(num, total));
                  else
                    CmdLine.SetProgress(Utility.GetProgress(num, total));
                }
                while (count > 0);
              }
            }
            catch (IOException ex)
            {
              Logger.WriteLog("Error: Can't access output file " + saveTo, false);
              if (GUI)
                Web.form.PauseDownload = true;
              Logger.WriteLog(ex.ToString(), false);
              return -1;
            }
            catch (WebException ex)
            {
              Logger.WriteLog("Error: Connection interrupted", false);
              Web.SetReconnect();
            }
            finally
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Stop);
              if (GUI)
                Web.form.lbl_speed.Invoke((Delegate) (() => Web.form.lbl_speed.Text = "0kB/s"));
            }
          }
        }
        return (int) responseFus.StatusCode;
      }
    }
  }
}
