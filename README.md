# SamFirm_Reborn
Based on https://github.com/ivanmeler/SamFirm_Reborn

Bug fixes:
- Fixed resume of download, when downloaded file has passed 2 GByte in size. Workaround for .NET HttpClient bug for Range header.

Changes:
- Switched to newAuth=1 => removed dependency to AgentModule.dll, CommonModule.dll and GlobalUtil.dll
- Switched to build for AnyCpu (since no need for 32-bit Samsung dll's)
- Builds with Visual Studio 2019 and .NET Framework 4.7.2
- Changed phone model and region to ComboBoxes, to have dropdown lists of previous downloaded firmwares.
- Downloaded models and regions stored to SamFirm.xml, to populate combo boxes.
- Auto unzipping while decrypting downloaded firmware file
- Saving metadata file FirmwareInfo.txt to folder for unzipped firmware files


Usage:

Windows GUI program
  Start without arguments

Console mode program:
  Start with command line arguments
Usage:

Update check:
     SamFirm.exe -c -model [device model] -region [region code]
                [-version [pda/csc/phone/data]] [-binary]

Decrypting:
     SamFirm.exe -file [path-to-file.zip.enc2] -version [pda/csc/phone/data] [-meta metafile]
     SamFirm.exe -file [path-to-file.zip.enc4] -version [pda/csc/phone/data] -logicValue [logicValue] [-meta metafile]

Downloading:
     SamFirm.exe -model [device model] -region [region code]
                [-version [pda/csc/phone/data]] [-folder [output folder]]
                [-binary] [-autodecrypt] [-nozip] [-meta metafile]
                
