# SamFirm_Reborn
Usage:

Windows GUI program
  Start without arguments

Console mode program:
  Start with command line arguments
Usage:

Update check:
     SamFirm.exe -c -model [device model] -region [region code] -imei [Imei or Serial number]
                [-version [pda/csc/phone/data]] [-binary]

Decrypting:
     SamFirm.exe -file [path-to-file.zip.enc2] -version [pda/csc/phone/data] [-meta metafile]
     SamFirm.exe -file [path-to-file.zip.enc4] -version [pda/csc/phone/data] -logicValue [logicValue] [-meta metafile]

Downloading:
     SamFirm.exe -model [device model] -region [region code] -imei [Imei or Serial number]
                [-version [pda/csc/phone/data]] [-folder [output folder]]
                [-binary] [-autodecrypt] [-nozip] [-meta metafile]
                
