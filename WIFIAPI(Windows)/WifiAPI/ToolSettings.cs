/****
 *chxiao
 ****/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HNAP;
using Utility;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
//using LTPControls;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;

namespace WifiAPI
{

    public class MapCaseAndNode
    {
        public string ModuleName;
        //public TestCase Case;
        public TreeNode Node;
    }

    public enum TOOL_STATUS { RUNNING, IDLE, STOP }

    public class DUTSpec
    {
        public int MACCap = 50;
        public string RootURL = "192.168.1.1";
        public DeviceInfo DeviceInfo = null;
        public string DUTUserName = "admin";
        public string DUTPassword = "admin";
    }

    [Serializable]
    public static class ToolSettings
    {
        public static string LogDirPre = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "log");
        public static string LogDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "log");
        public static List<MapCaseAndNode> Map = new List<MapCaseAndNode>(0);
        public static RadiusInfo RadiusSettings = new RadiusInfo();
        public static DUTSpec DUTCap = new DUTSpec();
        public static string Log4Setup = "start";
        public static string Log4Cleanup = "end";
        public static string ResourcePath = Path.Combine(Application.StartupPath, @"Resource");
        public static string LanPackPath = @"Resource\lan_pack.xml";
        public static string IndexFile = "index.html";
        //public static UPGRADE_SETTINGS UpgradeSettings = UPGRADE_SETTINGS.STRATUP;
        /*
        public static MapCaseAndNode GetMapNode(TestCase tc)
        {
            foreach (MapCaseAndNode man in ToolSettings.Map)
            {
                if (man.Case == tc)
                {
                    return man;
                }
            }
            return null;
        }
        */
        public static MapCaseAndNode GetMapNode(TreeNode node)
        {
            foreach (MapCaseAndNode man in ToolSettings.Map)
            {
                if (man.Node == node)
                {
                    return man;
                }
            }
            return null;
        }

        public static NetworkConnection NC = new NetworkConnection();
    }

    public static class UpateServer
    {

        static public string IP = "10.74.52.135";
        static public string Dir = "linksys/chxiao/LTP";
        static public string UserName = "test";
        static public string Password = "test123";
    }

    public static class ToolVersion
    {
        static public string Name = "LTP";
        static public string VER = "1.0";
        static public string BETA = "1";
        static public string BUILD = "35";
    }

    public static class SuiteParameterSettings
    {
        static public PhysicalAddress WLPrimaryMAC = PhysicalAddress.Parse("30-46-9A-F9-F0-B7");
        static public PhysicalAddress WiredPrimaryMAC = PhysicalAddress.Parse("00-11-0F-10-EA-1F");
    }

}
