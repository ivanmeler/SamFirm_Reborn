using System;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Xml
  {
    private static string BinaryInit = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><FUSMsg>\r\n\t<FUSHdr>\r\n\t\t<ProtoVer>1</ProtoVer>\r\n\t\t<SessionID>0</SessionID>\r\n\t\t<MsgID>1</MsgID>\r\n\t</FUSHdr>\r\n\t<FUSBody>\r\n\t\t<Put>\r\n\t\t\t<CmdID>1</CmdID>\r\n\t\t\t<BINARY_FILE_NAME>\r\n\t\t\t\t<Data>SM-T805_AUT_1_20140929155250_b8l0mvlbba_fac.zip.enc2</Data>\r\n\t\t\t</BINARY_FILE_NAME>\r\n\t\t\t<BINARY_NATURE>\r\n\t\t\t\t<Data>0</Data>\r\n\t\t\t</BINARY_NATURE>\r\n\t\t\t<BINARY_VERSION>\r\n\t\t\t\t<Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>\r\n\t\t\t</BINARY_VERSION>\r\n\t\t\t<DEVICE_LOCAL_CODE>\r\n\t\t\t\t<Data>AUT</Data>\r\n\t\t\t</DEVICE_LOCAL_CODE>\r\n\t\t\t<DEVICE_MODEL_TYPE>\r\n\t\t\t\t<Data>9</Data>\r\n\t\t\t</DEVICE_MODEL_TYPE>\r\n            <LOGIC_CHECK>\r\n                <Data>805XXU1ANFU1ANXX</Data>\r\n            </LOGIC_CHECK>\r\n\t\t</Put>\r\n\t\t<Get>\r\n\t\t\t<CmdID>2</CmdID>\r\n\t\t\t<LATEST_FW_VERSION/>\r\n\t\t</Get>\r\n\t</FUSBody>\r\n</FUSMsg>";
    private static string LatestVer = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><FUSMsg>\r\n\t<FUSHdr>\r\n\t\t<ProtoVer>1</ProtoVer>\r\n\t\t<SessionID>0</SessionID>\r\n\t\t<MsgID>1</MsgID>\r\n\t</FUSHdr>\r\n\t<FUSBody>\r\n\t\t<Put>\r\n\t\t\t<CmdID>1</CmdID>\r\n\t\t\t<ACCESS_MODE>\r\n\t\t\t\t<Data>2</Data>\r\n\t\t\t</ACCESS_MODE>\r\n\t\t\t<BINARY_NATURE>\r\n\t\t\t\t<Data>0</Data>\r\n\t\t\t</BINARY_NATURE>\r\n\t\t\t<CLIENT_LANGUAGE>\r\n\t\t\t\t<Type>String</Type>\r\n\t\t\t\t<Type>ISO 3166-1-alpha-3</Type>\r\n\t\t\t\t<Data>1033</Data>\r\n\t\t\t</CLIENT_LANGUAGE>\r\n\t\t\t<CLIENT_PRODUCT>\r\n\t\t\t\t<Data>Smart Switch</Data>\r\n\t\t\t</CLIENT_PRODUCT>\r\n\t\t\t<CLIENT_VERSION>\r\n\t\t\t\t<Data>4.1.16014_12</Data>\r\n\t\t\t</CLIENT_VERSION>\r\n\t\t\t<DEVICE_CONTENTS_DATA_VERSION>\r\n\t\t\t\t<Data>T805XXU1ANF6</Data>\r\n\t\t\t</DEVICE_CONTENTS_DATA_VERSION>\r\n\t\t\t<DEVICE_CSC_CODE2_VERSION>\r\n\t\t\t\t<Data>T805AUT1ANF1</Data>\r\n\t\t\t</DEVICE_CSC_CODE2_VERSION>\r\n\t\t\t<DEVICE_FW_VERSION>\r\n\t\t\t\t<Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>\r\n\t\t\t</DEVICE_FW_VERSION>\r\n\t\t\t<DEVICE_LOCAL_CODE>\r\n\t\t\t\t<Data>AUT</Data>\r\n\t\t\t</DEVICE_LOCAL_CODE>\r\n\t\t\t<DEVICE_MODEL_NAME>\r\n\t\t\t\t<Data>SM-T805</Data>\r\n\t\t\t</DEVICE_MODEL_NAME>\r\n\t\t\t<DEVICE_PDA_CODE1_VERSION>\r\n\t\t\t\t<Data>T805XXU1ANE6</Data>\r\n\t\t\t</DEVICE_PDA_CODE1_VERSION>\r\n\t\t\t<DEVICE_PHONE_FONT_VERSION>\r\n\t\t\t\t<Data>T805XXU1ANF6</Data>\r\n\t\t\t</DEVICE_PHONE_FONT_VERSION>\r\n\t\t\t<DEVICE_PLATFORM>\r\n\t\t\t\t<Data>Android</Data>\r\n\t\t\t</DEVICE_PLATFORM>\r\n            <LOGIC_CHECK>\r\n                <Data>805XXU1ANFU1ANXX</Data>\r\n            </LOGIC_CHECK>\r\n\t\t</Put>\r\n\t\t<Get>\r\n\t\t\t<CmdID>2</CmdID>\r\n\t\t\t<LATEST_FW_VERSION/>\r\n\t\t</Get>\r\n\t</FUSBody>\r\n</FUSMsg>";

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
      xelement.Element((XName) "LOGIC_CHECK").Element((XName) "Data").Value = Utility.GetLogicCheck(pdaver + "/" + cscver + "/" + phonever + "/" + dataver, Web.Nonce);
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
