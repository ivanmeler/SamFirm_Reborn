// Decompiled with JetBrains decompiler
// Type: SamFirm.Error
// Assembly: SamFirm, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 14A8B9D4-ACD6-4CE0-9F53-A466F0519E6A
// Assembly location: C:\Users\Ivan\Desktop\LG Flash Tool 2014\SamFirm\SamFirm.exe

namespace SamFirm
{
  public enum Error
  {
    NoError = 0,
    Generic = 1,
    UpdateCheckError = 2,
    DecryptError = 3,
    DownloadError = 4,
    AutoFetchError = 5,
    FIPSComplianceError = 800, // 0x00000320
    ResponseStreamError = 900, // 0x00000384
    NullResponse = 901, // 0x00000385
  }
}
