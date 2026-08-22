# SamFirm_Reborn
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
                [-binary] [-autodecrypt | -legacydecrypt | -nodecrypt] [-nozip] [-meta metafile]

Samsung wearable models are supported. With decryption enabled (the default),
encrypted firmware is decrypted while it is downloaded. The GUI saves the
decrypted firmware as a `.zip`; it does not extract it automatically. Use
`-nodecrypt` to keep the encrypted package, or `-nozip` to keep the decrypted
package without extracting it in console mode.
Use `-legacydecrypt` to download the encrypted package with resumable ranges,
then decrypt it after the download completes.
                
