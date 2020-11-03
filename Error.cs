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
