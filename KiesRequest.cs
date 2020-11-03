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
      httpWebRequest.UserAgent = "Kies2.0_FUS";
      httpWebRequest.Headers.Add("Authorization", "FUS nonce=\"\", signature=\"\", nc=\"\", type=\"\", realm=\"\"");
      CookieContainer cookieContainer = new CookieContainer(1);
      Cookie cookie = new Cookie("JSESSIONID", Web.JSessionID);
      cookieContainer.Add(new Uri(requestUriString), cookie);
      httpWebRequest.CookieContainer = cookieContainer;
      return httpWebRequest;
    }
  }
}
