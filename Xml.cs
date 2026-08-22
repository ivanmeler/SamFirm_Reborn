using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SamFirm
{
  internal class Xml
  {
    private static string BinaryInit = @"<FUSMsg>
    <FUSHdr>
        <ProtoVer>1.0</ProtoVer>
        <SessionID>0</SessionID>
        <MsgID>1</MsgID>
    </FUSHdr>
    <FUSBody>
        <Put>
            <BINARY_NAME>
                <Data>SM-T805_AUT_1_20140929155250_b8l0mvlbba_fac.zip.enc2</Data>
            </BINARY_NAME>
            <BINARY_SW_VERSION>
                <Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>
            </BINARY_SW_VERSION>
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
    </FUSBody>
</FUSMsg>";

    private static string LatestVer = @"<FUSMsg>
    <FUSHdr>
        <ProtoVer>1.0</ProtoVer>
        <SessionID>0</SessionID>
        <MsgID>1</MsgID>
    </FUSHdr>
    <FUSBody>
        <Put>
            <CmdID>1</CmdID>
            <ACCESS_MODE>
                <Data>1</Data>
            </ACCESS_MODE>
            <BINARY_NATURE>
                <Data>1</Data>
            </BINARY_NATURE>
            <REQUEST_TYPE>
                <Data>2</Data>
            </REQUEST_TYPE>
            <LOGIC_CHECK>
                <Data>805XXU1ANFU1ANXX</Data>
            </LOGIC_CHECK>
            <BINARY_SW_VERSION>
                <Data>T805XXU1ANFB/T805AUT1ANF1/T805XXU1ANF6/T805XXU1ANFB</Data>
            </BINARY_SW_VERSION>
            <BINARY_LOCAL_CODE>
                <Data>AUT</Data>
            </BINARY_LOCAL_CODE>
            <BINARY_MODEL_NAME>
                <Data>SM-T805</Data>
            </BINARY_MODEL_NAME>
        </Put>
        <Get>
            <CmdID>2</CmdID>
            <BINARY_SW_VERSION/>
        </Get>
    </FUSBody>
</FUSMsg>";

    public static string GetXMLValue(
      string xml,
      string element,
      string attributename = null,
      string attributevalue = null)
    {
      if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(element))
        return string.Empty;

      XDocument xdocument = XDocument.Parse(xml);
      string[] strArray = element.Split('/');
      XElement xelement = xdocument.Root;
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (xelement == null)
          return string.Empty;

        if (index < strArray.Length - 1)
        {
          xelement = xelement.Element((XName)strArray[index]);
        }
        else
        {
          XElement found = null;
          foreach (XElement element1 in xelement.Elements((XName)strArray[index]))
          {
            if (attributename == null)
            {
              found = element1;
              break;
            }
            XAttribute xattribute = element1.Attribute((XName)attributename);
            if (xattribute != null && (attributevalue == null || xattribute.Value == attributevalue))
            {
              found = element1;
              break;
            }
          }
          xelement = found;
        }
      }
      return xelement?.Value ?? string.Empty;
    }

    public static List<KeyValuePair<string, string>> GetXMLDataValues(string xml)
    {
      List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
      if (string.IsNullOrEmpty(xml))
        return values;

      XDocument xdocument = XDocument.Parse(xml);
      foreach (XElement data in xdocument.Descendants((XName)"Data"))
      {
        if (data.Parent == null || string.IsNullOrEmpty(data.Value))
          continue;
        values.Add(new KeyValuePair<string, string>(data.Parent.Name.LocalName, data.Value));
      }
      return values;
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
      string firmwareVersion = pdaver + "/" + cscver + "/" + phonever + "/" + dataver;
      XDocument xdocument = XDocument.Parse(SamFirm.Xml.LatestVer);
      XElement xelement = xdocument.Element((XName)"FUSMsg").Element((XName)"FUSBody").Element((XName)"Put");
      SetDataValue(xelement, "BINARY_MODEL_NAME", model);
      SetDataValue(xelement, "BINARY_LOCAL_CODE", region);
      SetDataValue(xelement, "BINARY_SW_VERSION", firmwareVersion);
      SetDataValue(xelement, "LOGIC_CHECK", Utility.GetLogicCheck(firmwareVersion, Web.Nonce));
      return xdocument.ToString();
    }

    public static string GetXmlBinaryInit(
      string file,
      string version,
      string region,
      string model_type)
    {
      XDocument xdocument = XDocument.Parse(SamFirm.Xml.BinaryInit);
      XElement xelement = xdocument.Element((XName)"FUSMsg").Element((XName)"FUSBody").Element((XName)"Put");
      SetDataValue(xelement, "BINARY_NAME", file);
      SetDataValue(xelement, "BINARY_SW_VERSION", version);
      SetDataValue(xelement, "DEVICE_LOCAL_CODE", region);
      SetDataValue(xelement, "DEVICE_MODEL_TYPE", model_type);

      string checkInput = file;
      if ((file.EndsWith(".zip.enc2") || file.EndsWith(".zip.enc4")) && file.Length >= 25)
        checkInput = file.Substring(file.Length - 25, 16);

      SetDataValue(xelement, "LOGIC_CHECK", Utility.GetLogicCheck(checkInput, Web.Nonce));
      return xdocument.ToString();
    }

    private static void SetDataValue(XElement parent, string name, string value)
    {
      parent.Element((XName)name).Element((XName)"Data").Value = value;
    }
  }
}
