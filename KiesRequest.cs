// Decompiled with JetBrains decompiler
// Type: SamFirm.KiesRequest
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

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
