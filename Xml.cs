using System;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Xml
  {
        private static string BinaryInit = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<FUSMsg>
    <FUSHdr>
        <ProtoVer>1</ProtoVer>
        <SessionID>0</SessionID>
        <MsgID>1</MsgID>
    </FUSHdr>
    <FUSBody>
        <Put>
            <CmdID>1</CmdID>
            <BINARY_FILE_NAME>
                <Data>SM-T805_AUT_1_20140929155250_b8l0mvlbba_fac.zip.enc2</Data>
            </BINARY_FILE_NAME>
            <BINARY_NATURE>
                <Data>0</Data>
            </BINARY_NATURE>
            <BINARY_VERSION>
                <Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>
            </BINARY_VERSION>
            <DEVICE_LOCAL_CODE>
                <Data>AUT</Data>
            </DEVICE_LOCAL_CODE>
            <DEVICE_MODEL_TYPE>
                <Data>9</Data>
            </DEVICE_MODEL_TYPE>
            <LOGIC_CHECK>
                <Data>805XXU1ANFU1ANXX</Data>
            </LOGIC_CHECK>
        </Put>
        <Get>
            <CmdID>2</CmdID>
            <LATEST_FW_VERSION/>
        </Get>
    </FUSBody>
</FUSMsg>";

        private static string LatestVer = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<FUSMsg>
    <FUSHdr>
        <ProtoVer>1</ProtoVer>
        <SessionID>0</SessionID>
        <MsgID>1</MsgID>
    </FUSHdr>
    <FUSBody>
        <Put>
            <CmdID>1</CmdID>
            <ACCESS_MODE>
                <Data>2</Data>
            </ACCESS_MODE>
            <BINARY_NATURE>
                <Data>0</Data>
            </BINARY_NATURE>
            <CLIENT_LANGUAGE>
                <Type>String</Type>
                <Type>ISO 3166-1-alpha-3</Type>
                <Data>1033</Data>
            </CLIENT_LANGUAGE>
            <CLIENT_PRODUCT>
                <Data>Smart Switch</Data>
            </CLIENT_PRODUCT>
            <CLIENT_VERSION>
                <Data>4.3.23123_1</Data>
            </CLIENT_VERSION>
            <DEVICE_CONTENTS_DATA_VERSION>
                <Data>T805XXU1ANF6</Data>
            </DEVICE_CONTENTS_DATA_VERSION>
            <DEVICE_CSC_CODE2_VERSION>
                <Data>T805AUT1ANF1</Data>
            </DEVICE_CSC_CODE2_VERSION>
            <DEVICE_FW_VERSION>
                <Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>
            </DEVICE_FW_VERSION>
            <DEVICE_IMEI_PUSH>
                <Data>00000000000000</Data>
            </DEVICE_IMEI_PUSH>
            <DEVICE_LOCAL_CODE>
                <Data>AUT</Data>
            </DEVICE_LOCAL_CODE>
            <DEVICE_MODEL_NAME>
                <Data>SM-T805</Data>
            </DEVICE_MODEL_NAME>
            <DEVICE_PDA_CODE1_VERSION>
                <Data>T805XXU1ANE6</Data>
            </DEVICE_PDA_CODE1_VERSION>
            <DEVICE_PHONE_FONT_VERSION>
                <Data>T805XXU1ANF6</Data>
            </DEVICE_PHONE_FONT_VERSION>
            <DEVICE_PLATFORM>
                <Data>Android</Data>
            </DEVICE_PLATFORM>
            <LOGIC_CHECK>
                <Data>805XXU1ANFU1ANXX</Data>
            </LOGIC_CHECK>
        </Put>
        <Get>
            <CmdID>2</CmdID>
            <LATEST_FW_VERSION/>
        </Get>
    </FUSBody>
</FUSMsg>";

    public static string GetXMLValue(
      string xml,
      string element,
      string attributename = null,
      string attributevalue = null)
    {
      XDocument xdocument = XDocument.Parse(xml);
      string[] strArray = element.Split('/');
      XElement xelement = xdocument.Root;
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (index < strArray.Length - 1)
        {
          xelement = xelement.Element((XName) strArray[index]);
        }
        else
        {
          foreach (XElement element1 in xelement.Elements((XName) strArray[index]))
          {
            if (attributename == null)
            {
              xelement = element1;
              break;
            }
            XAttribute xattribute = element1.Attribute((XName) attributename);
            if (xattribute != null && (attributevalue == null || !(xattribute.Value != attributevalue)))
            {
              xelement = element1;
              break;
            }
          }
        }
      }
      return xelement.Value;
    }

    public static string GetXmlBinaryInform(
      string model,
      string region,
      string pdaver,
      string cscver,
      string phonever,
      string dataver,
      bool BinaryNature = false)
    {
      XDocument xdocument = XDocument.Parse(SamFirm.Xml.LatestVer);
      XElement xelement = xdocument.Element((XName) "FUSMsg").Element((XName) "FUSBody").Element((XName) "Put");
      xelement.Element((XName) "DEVICE_MODEL_NAME").Element((XName) "Data").Value = model;
      xelement.Element((XName) "DEVICE_LOCAL_CODE").Element((XName) "Data").Value = region;
      xelement.Element((XName) "DEVICE_CONTENTS_DATA_VERSION").Element((XName) "Data").Value = dataver;
      xelement.Element((XName) "DEVICE_CSC_CODE2_VERSION").Element((XName) "Data").Value = cscver;
      xelement.Element((XName) "DEVICE_PDA_CODE1_VERSION").Element((XName) "Data").Value = pdaver;
      xelement.Element((XName) "DEVICE_PHONE_FONT_VERSION").Element((XName) "Data").Value = phonever;
      xelement.Element((XName) "DEVICE_FW_VERSION").Element((XName) "Data").Value = pdaver + "/" + cscver + "/" + phonever + "/" + dataver;
      xelement.Element((XName) "BINARY_NATURE").Element((XName) "Data").Value = Convert.ToInt32(BinaryNature).ToString();
      //xelement.Element((XName) "LOGIC_CHECK").Element((XName) "Data").Value = Utility.GetLogicCheck(pdaver + "/" + cscver + "/" + phonever + "/" + dataver, Web.Nonce);
      xelement.Element((XName) "LOGIC_CHECK").Element((XName) "Data").Value = Utility.GetLogicCheck(pdaver + "/" + cscver + "/" + phonever + "/" + dataver, Web.Nonce);

      //hardcode EUX as Germany and EUY as Republic of Serbia
      if (region == "EUX")
      {
        xelement.Add(
            new XElement("DEVICE_AID_CODE",
                new XElement("Data", region)),
            new XElement("DEVICE_CC_CODE",
                new XElement("Data", "DE")),
            new XElement("MCC_NUM",
                new XElement("Data", "262")),
            new XElement("MNC_NUM",
                new XElement("Data", "01"))
        );
      }
      else if (region == "EUY")
      {
        xelement.Add(
            new XElement("DEVICE_AID_CODE",
                new XElement("Data", region)),
            new XElement("DEVICE_CC_CODE",
                new XElement("Data", "RS")),
            new XElement("MCC_NUM",
                new XElement("Data", "220")),
            new XElement("MNC_NUM",
                new XElement("Data", "01"))
        );
      }
      return xdocument.ToString();
    }

    public static string GetXmlBinaryInit(
      string file,
      string version,
      string region,
      string model_type)
    {
      XDocument xdocument = XDocument.Parse(SamFirm.Xml.BinaryInit);
      XElement xelement = xdocument.Element((XName) "FUSMsg").Element((XName) "FUSBody").Element((XName) "Put");
      xelement.Element((XName) "BINARY_FILE_NAME").Element((XName) "Data").Value = file;
      xelement.Element((XName) "BINARY_VERSION").Element((XName) "Data").Value = version;
      xelement.Element((XName) "DEVICE_LOCAL_CODE").Element((XName) "Data").Value = region;
      xelement.Element((XName) "DEVICE_MODEL_TYPE").Element((XName) "Data").Value = model_type;
      xelement.Element((XName) "LOGIC_CHECK").Element((XName) "Data").Value = Utility.GetLogicCheck(file, Web.Nonce);
      return xdocument.ToString();
    }
  }
}
