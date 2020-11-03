using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SamFirm
{
  public static class WebRequestExtension
  {
    public static WebResponse GetResponseFUS(this WebRequest wr)
    {
      try
      {
        WebResponse response = wr.GetResponse();
        if (((IEnumerable<string>) response.Headers.AllKeys).Contains<string>("Set-Cookie"))
          Web.JSessionID = WebRequestExtension.seal(response.Headers[HttpResponseHeader.SetCookie], "JSESSIONID");
        if (((IEnumerable<string>) response.Headers.AllKeys).Contains<string>("NONCE"))
          Web.Nonce = response.Headers["NONCE"];
        return response;
      }
      catch (WebException ex)
      {
        Logger.WriteLog("Error getting response: " + ex.Message, false);
        if (ex.Status == WebExceptionStatus.NameResolutionFailure)
          Web.SetReconnect();
        return ex.Response;
      }
    }
    private static string seal(string tuljan, string _param1)
    {
      string[] strArray = tuljan.Split(new char[2]{ ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
      string empty = string.Empty;
      foreach (string str in strArray)
      {
        if (str.Contains(_param1))
          empty = str.Split(new string[1]{ "=" }, StringSplitOptions.RemoveEmptyEntries)[1];
      }
      return empty;
    }
  }
}
