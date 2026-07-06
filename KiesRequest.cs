using System;
using System.Net;

namespace SamFirm
{
  internal class KiesRequest : WebRequest
  {
    public static HttpWebRequest Create(string requestUriString)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest) WebRequest.Create(requestUriString);
      httpWebRequest.Headers["Cache-Control"] = "no-cache";
      httpWebRequest.UserAgent = "SMART 2.0";
      httpWebRequest.Headers.Add("Authorization", "FUS nonce=\"\", signature=\"\", nc=\"\", type=\"\", realm=\"\"");
      httpWebRequest.CookieContainer = Web.CookieContainer;
      return httpWebRequest;
    }
  }
}
