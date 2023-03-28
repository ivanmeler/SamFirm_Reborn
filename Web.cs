using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SamFirm
{
  internal class Web
  {
    public static string JSessionID = string.Empty;
    public static string EncryptedNonce = string.Empty;
    public static byte[] DecryptedNonce = null;
    public static string Nonce => Encoding.UTF8.GetString(DecryptedNonce);
    public static string Auth = string.Empty;
    public static string AuthHeaderWithNonce => $"FUS nonce=\"{EncryptedNonce}\", signature=\"{Auth}\", nc=\"\", type=\"\", realm=\"\", newauth=\"1\"";
    public static string AuthHeaderNoNonce => $"FUS nonce=\"\", signature=\"{Auth}\", nc=\"\", type=\"\", realm=\"\", newauth=\"1\"";
    public static Form1 form;

    public static int GenerateNonce()
    {
      HttpWebRequest wr = KiesRequest.Create("https://neofussvr.sslcs.cdngc.net/NF_DownloadGenerateNonce.do");
      wr.Method = "POST";
      string authv = $"FUS nonce=\"\", signature=\"\", nc=\"\", type=\"\", realm=\"\", newauth=\"1\"";
      wr.Headers["Authorization"] = authv;
      wr.ContentLength = 0L;
      using (HttpWebResponse responseFus = (HttpWebResponse)wr.GetResponseFUS())
      {
        if (responseFus == null)
          return 901;
        int statusCode = (int)responseFus.StatusCode;
        return statusCode;
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
      xmlresponse = (string)null;
      HttpWebRequest wr = KiesRequest.Create(URL);
      wr.Method = "POST";
      wr.Headers["Authorization"] = AuthHeaderNoNonce;
      byte[] bytes = Encoding.ASCII.GetBytes(Regex.Replace(xml, "\\r\\n?|\\n|\\t", string.Empty));
      wr.ContentLength = (long)bytes.Length;
      using (Stream requestStream = wr.GetRequestStream())
        requestStream.Write(bytes, 0, bytes.Length);
      using (HttpWebResponse responseFus = (HttpWebResponse)wr.GetResponseFUS())
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
        return (int)responseFus.StatusCode;
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
      long bytesTransferred = 0;
      HttpWebRequest wr = KiesRequest.Create("http://cloud-neofussvr.sslcs.cdngc.net/NF_DownloadBinaryForMass.do?file=" + path + file);
      wr.Method = "GET";
      wr.Headers["Authorization"] = AuthHeaderWithNonce;
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
        //wr.AddRange((int) length); // This creates an error, if download continues when file size has exceeded 2 GByte, since AddRange then adds a negative range!
        wr.AddRange(length);  // Fixed using an extension, handling range typed as long
        bytesTransferred = length;
      }
      using (HttpWebResponse responseFus = (HttpWebResponse)wr.GetResponseFUS())
      {
        if (responseFus == null)
        {
          Logger.WriteLog("Error downloading: response is null", false);
          return 901;
        }
        if (responseFus.StatusCode != HttpStatusCode.OK && responseFus.StatusCode != HttpStatusCode.PartialContent)
        {
          Logger.WriteLog("Error downloading: " + (object)(int)responseFus.StatusCode, false);
        }
        else
        {
          long total = long.Parse(responseFus.GetResponseHeader("content-length")) + bytesTransferred;
          if (!System.IO.File.Exists(saveTo) || new FileInfo(saveTo).Length != total)
          {
            byte[] buffer = new byte[256 * 1024];
            Stopwatch sw = new Stopwatch();
            Utility.ResetSpeed(bytesTransferred);
            try
            {
              Utility.PreventDeepSleep(Utility.PDSMode.Start);
              using (BinaryWriter binaryWriter = new BinaryWriter((Stream)new FileStream(saveTo, FileMode.Append)))
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
                  bytesTransferred += (long)(count = responseFus.GetResponseStream().Read(buffer, 0, buffer.Length));
                  if (count > 0)
                  {
                    binaryWriter.Write(buffer, 0, count);
                    if (GUI)
                    {
                      int dlspeed = Utility.DownloadSpeed(bytesTransferred, sw);
                      if (dlspeed != -1)
                        Web.form.lbl_speed.Invoke((Delegate)((Action)(() => Web.form.lbl_speed.Text = dlspeed.ToString() + " KB/s")));
                    }
                  }
                  if (GUI)
                    Web.form.SetProgressBar(Utility.GetProgress(bytesTransferred, total), bytesTransferred);
                  else
                    CmdLine.SetProgress(Utility.GetProgress(bytesTransferred, total));
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
                Web.form.lbl_speed.Invoke((Delegate)((Action)(() => Web.form.lbl_speed.Text = "0 KB/s")));
            }
          }
        }
        return (int)responseFus.StatusCode;
      }
    }
  }

  public static class Extensions
  {
    static MethodInfo httpWebRequestAddRangeHelper = typeof(WebHeaderCollection).GetMethod
                                    ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
    /// <summary>
    /// Adds a byte range header to a request for a specific range from the beginning or end of the requested data.
    /// </summary>
    /// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
    /// <param name="start">The starting or ending point of the range.</param>
    public static void AddRange(this HttpWebRequest request, long start) { request.AddRange(start, -1L); }

    /// <summary>Adds a byte range header to the request for a specified range.</summary>
    /// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
    /// <param name="start">The position at which to start sending data.</param>
    /// <param name="end">The position at which to stop sending data.</param>
    public static void AddRange(this HttpWebRequest request, long start, long end)
    {
      if (request == null) throw new ArgumentNullException("request");
      if (start < 0) throw new ArgumentOutOfRangeException("start", "Starting byte cannot be less than 0.");
      if (end < start) end = -1;

      string key = "Range";
      string val = string.Format("bytes={0}-{1}", start, end == -1 ? "" : end.ToString());

      httpWebRequestAddRangeHelper.Invoke(request.Headers, new object[] { key, val });
    }
  }
}
