using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NativeWifi;
using System.Xml;
//using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Management;
using System.Diagnostics;
using Utility;
using HNAP;
//using Shell32;

using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
//using HardwareHelperLib;

namespace WifiAPI
{
    public enum INTERFACE_TYPE
    {
        WIRED,
        WIRELESS,
        ALL
    };
    public class NetworkInterfaceInfoEx
    {
        public string MAC;
        public INTERFACE_TYPE Type;
        public WlanClient.WlanInterface WirelessInterface;
    }

    public class NetworkConnection
    {
        public enum TYPE
        {
            WIRED,
            WIRELESS,
            ALL
        };

        [DllImport("Ndis.dll", SetLastError = true)]
        private static extern void NdisBindProtocolsToAdapter
        (
        ref int pStatus,
        ref string wszAdapterInstanceName,
        ref string wszProtocolName
        );

        [DllImport("Ndis.dll", SetLastError = true)]
        private static extern void NdisUnbindProtocolsFromAdapter
        (
        ref int pStatus,
        ref string wszAdapterInstanceName,
        ref string wszProtocolName
        );

        NetworkInterfaceInfoEx PrimaryWLA = new NetworkInterfaceInfoEx();
        NetworkInterfaceInfoEx WiredA = new NetworkInterfaceInfoEx();

        private WlanClient WirelessClient = null;
        public int ConnectionTimeOut = 10000;
        //public int ConnectionTimeOut = 45000;
        private string FakeIPAddr = "2.2.2.2";
        private string FakeNetMask = "255.255.255.0";
        //some cost down router need take a long time to check,so check for serval time
        private int CheckCount = 2;
        private int NICSettingEffectTime = 3;

        public NetworkConnection()
        {
            WirelessClient = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                wlanIface.DisConnect();
                ClearWirelessProfile(wlanIface);
                wlanIface.Scan();
            }
            InitAllAdapter();
        }

        public bool ResetWirelessConnection(PhysicalAddress mac)
        {
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                wlanIface.DisConnect();
                ClearWirelessProfile(wlanIface);
            }
            return true;
        }
        public bool ResetWirelessConnection()
        {
            WirelessClient = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                wlanIface.DisConnect();
                ClearWirelessProfile(wlanIface);
            }
            return true;
        }

        public List<NetworkInterface> GetWirelessNICList()
        {
            List<NetworkInterface> ni = new List<NetworkInterface> { };
            WirelessClient = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                ni.Add(wlanIface.NetworkInterface);
            }
            return ni;
        }

        public WlanClient.WlanInterface FoundWlanIf(PhysicalAddress mac)
        {
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                PhysicalAddress mac1 = wlanIface.NetworkInterface.GetPhysicalAddress();
                if (mac1.ToString() == mac.ToString())
                {
                    return wlanIface;
                }
            }
            return null;
        }

        public NetworkInterface GetNetworkInferface(PhysicalAddress mac)
        {
            List<NetworkInterface> Interfaces = GetNetworkAdapters(TYPE.ALL);
            foreach (NetworkInterface ni in Interfaces)
            {
                if (ni.GetPhysicalAddress().ToString() == mac.ToString())
                {
                    return ni;
                }
            }
            return null;
        }

        public IPv4Info GetIPv4InfoFromNetworkAdapter(NetworkInterface Intface)
        {

            IPv4Info IntfaceInfo = new IPv4Info();
            IPInterfaceProperties properties = Intface.GetIPProperties();
            UnicastIPAddressInformationCollection uniCast = properties.UnicastAddresses;

            if (uniCast.Count > 0)
            {
                foreach (UnicastIPAddressInformation uni in uniCast)
                {
                    if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {

                        IntfaceInfo.IPAddr = uni.Address.ToString();
                        if (uni.IPv4Mask != null)
                        {
                            IntfaceInfo.NetMask = uni.IPv4Mask.ToString();
                        }
                        else
                        {
                            IntfaceInfo.NetMask = "0.0.0.0";
                        }

                    }
                }
            }
            else
            {
                IntfaceInfo.IPAddr = "0.0.0.0";
                IntfaceInfo.NetMask = "0.0.0.0";
            }
            GatewayIPAddressInformationCollection gateWay = properties.GatewayAddresses;
            if (gateWay.Count > 0)
            {
                IntfaceInfo.Gateway = gateWay[0].Address.ToString();
            }
            else
            {
                IntfaceInfo.Gateway = "0.0.0.0";
            }

            IPAddressCollection dhcpServer = properties.DhcpServerAddresses;
            if (dhcpServer != null)
            {
                if (dhcpServer.Count > 0)
                {
                    IntfaceInfo.DHCPServer = dhcpServer[0].ToString();
                }
                else
                {
                    IntfaceInfo.DHCPServer = "0.0.0.0";
                }
            }

            IPAddressCollection dnsServer = properties.DnsAddresses;
            if (dnsServer != null)
            {
                if (dnsServer.Count > 0)
                {
                    IntfaceInfo.DNSServer = dnsServer[0].ToString();
                }
                else
                {
                    IntfaceInfo.DNSServer = "0.0.0.0";
                }
            }

            return IntfaceInfo;
        }

        public bool EnableDHCPonNetworkAdapter(NetworkInterface Intface)
        {
            ManagementBaseObject outPar = null;
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            IPv4Info IntfaceInfo = GetIPv4InfoFromNetworkAdapter(Intface);
            foreach (ManagementObject mo in moc)
            {
                if (!(bool)mo["IPEnabled"])
                {
                    continue;
                }
                if (((string)mo["MACAddress"]) != ConvertPhyAddrToMac(Intface.GetPhysicalAddress()))
                {
                    continue;
                }

                MiscLib.Debug("Found Interface (" + Intface.Name + ") to Enable DHCP");
                Program.loginfo.Info("Found Interface (" + Intface.Name + ") to Enable DHCP");
                Program.sw.WriteLine("Found Interface (" + Intface.Name + ") to Enable DHCP");
                /*
                MiscLib.Debug("SetDNSServerSearchOrder");
                Program.loginfo.Info("SetDNSServerSearchOrder");
                Program.sw.WriteLine("SetDNSServerSearchOrder");
                mo.InvokeMethod("SetDNSServerSearchOrder", null);
                Utility.MiscLib.WaitS(2);
                 */

                MiscLib.Debug("EnableDHCP");
                Program.loginfo.Info("EnableDHCP");
                Program.sw.WriteLine("EnableDHCP");
                outPar = mo.InvokeMethod("EnableDHCP", null, null);
                //Wait for 60 seconds for adapter to get the ip address from dhcp server
                for (int i = 0; i < CheckCount; i++)
                {
                    Utility.MiscLib.WaitS(2);
                    MiscLib.Debug("Refresh interface information");
                    Program.loginfo.Info("Refresh interface information");
                    Program.sw.WriteLine("Refresh interface information");
                    NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in adapters)
                    {
                        if (adapter.Id == Intface.Id)
                        {
                            MiscLib.Debug("Query IP information");
                            Program.loginfo.Info("Query IP information");
                            Program.sw.WriteLine("Query IP information");
                            IntfaceInfo = GetIPv4InfoFromNetworkAdapter(adapter);
                            if (IntfaceInfo == null
                                || IntfaceInfo.Gateway == "0.0.0.0"
                                || IntfaceInfo.IPAddr == "0.0.0.0" ||
                                IntfaceInfo.IPAddr == FakeIPAddr)
                            {
                                continue;
                            }
                            MiscLib.Debug("DHCP server is : " + IntfaceInfo.DHCPServer);
                            Program.loginfo.Info("DHCP server is : " + IntfaceInfo.DHCPServer);
                            Program.sw.WriteLine("DHCP server is : " + IntfaceInfo.DHCPServer);
                            MiscLib.Debug("IP address is : " + IntfaceInfo.IPAddr);
                            Program.loginfo.Info("IP address is : " + IntfaceInfo.IPAddr);
                            Program.sw.WriteLine("IP address is : " + IntfaceInfo.IPAddr);
                            //remove this since this will cause some adapter drive crashed
                            //mc.Dispose();
                            return true;
                        }
                    }
                }
            }
            MiscLib.Debug("Can not Get IP address for (" + Intface.Name + ")");
            Program.loginfo.Info("Can not Get IP address for (" + Intface.Name + ")");
            Program.sw.WriteLine("Can not Get IP address for (" + Intface.Name + ")");
            //mc.Dispose();
            return false;
        }

        private bool EnableStaticOnNetworkAdapter(NetworkInterface Intface, string IPAddr, string NetMask)
        {
            ManagementBaseObject inPar = null;
            ManagementBaseObject outPar = null;
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (!(bool)mo["IPEnabled"])
                {
                    continue;
                }
                if (((string)mo["MACAddress"]) != ConvertPhyAddrToMac(Intface.GetPhysicalAddress()))
                {
                    continue;
                }
                //set ip address and network mask
                inPar = mo.GetMethodParameters("EnableStatic");
                inPar["IPAddress"] = new string[] { IPAddr };
                inPar["SubnetMask"] = new string[] { NetMask };
                outPar = mo.InvokeMethod("EnableStatic", inPar, null);
                MiscLib.Debug("Set IP Address to : " + IPAddr);
                MiscLib.Debug("Set Net Mask to : " + NetMask);
                //mc.Dispose();
                return true;
            }
            //mc.Dispose();
            return false;
        }

        private bool SetGatewaysOnNetworkAdapter(NetworkInterface Intface, string Gateway)
        {
            ManagementBaseObject inPar = null;
            ManagementBaseObject outPar = null;
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (!(bool)mo["IPEnabled"])
                {
                    continue;
                }
                if (((string)mo["MACAddress"]) != ConvertPhyAddrToMac(Intface.GetPhysicalAddress()))
                {
                    continue;
                }
                //set ip address and network mask
                inPar = mo.GetMethodParameters("SetGateways");
                inPar["DefaultIPGateway"] = new string[] { Gateway };
                inPar["GatewayCostMetric"] = new int[] { 1 };
                outPar = mo.InvokeMethod("SetGateways", inPar, null);
                MiscLib.Debug("Set Gateway to : " + Gateway);
                Utility.MiscLib.WaitS(NICSettingEffectTime);
                //mc.Dispose();
                return true;
            }
            //mc.Dispose();
            return false;
        }

        private bool SetDNSServerSearchOrderOnNetworkAdapter(NetworkInterface Intface, string DNSServer)
        {
            ManagementBaseObject inPar = null;
            ManagementBaseObject outPar = null;
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (!(bool)mo["IPEnabled"])
                {
                    continue;
                }
                if (((string)mo["MACAddress"]) != ConvertPhyAddrToMac(Intface.GetPhysicalAddress()))
                {
                    continue;
                }
                //set dns server
                inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
                inPar["DNSServerSearchOrder"] = new string[] { DNSServer };
                outPar = mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
                MiscLib.Debug("Set DNSServer to : " + DNSServer);
                Utility.MiscLib.WaitS(NICSettingEffectTime);
                //mc.Dispose();
                return true;
            }
            //mc.Dispose();
            return false;
        }

        public void SetIPv4InfoOnNetworkAdapters(NetworkInterface Intface, IPv4Info NetworkInfo)
        {
            if (!string.IsNullOrEmpty(NetworkInfo.IPAddr) && !string.IsNullOrEmpty(NetworkInfo.NetMask))
            {
                //Utility.MiscLib.Debug("Set IP to " + NetworkInfo.IPAddr);
                //Utility.MiscLib.Debug("Set MASK to " + NetworkInfo.NetMask);
                EnableStaticOnNetworkAdapter(Intface, NetworkInfo.IPAddr, NetworkInfo.NetMask);
            }
            if (!string.IsNullOrEmpty(NetworkInfo.Gateway))
            {
                //Utility.MiscLib.Debug("Set Gateway to " + NetworkInfo.Gateway);
                SetGatewaysOnNetworkAdapter(Intface, NetworkInfo.Gateway);
            }
            if (!string.IsNullOrEmpty(NetworkInfo.DNSServer))
            {
                //Utility.MiscLib.Debug("Set DNS to " + NetworkInfo.DNSServer);
                SetDNSServerSearchOrderOnNetworkAdapter(Intface, NetworkInfo.DNSServer);
            }
        }

        public List<NetworkInterface> GetNetworkAdapters(TYPE AdapterType)
        {
            List<NetworkInterface> nics = new List<NetworkInterface>();


            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            if (AdapterType == TYPE.ALL)
            {
                foreach (NetworkInterface adapter in adapters)
                {
                    //no fibre in our PC
                    if ((adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        || (adapter.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                        || (adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT)
                        || (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        nics.Add(adapter);
                    }
                }
            }
            else if (AdapterType == TYPE.WIRELESS)
            {
                foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
                {
                    nics.Add(wlanIface.NetworkInterface);
                }
            }
            else
            {
                List<NetworkInterface> wlnics = new List<NetworkInterface>();

                foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
                {
                    wlnics.Add(wlanIface.NetworkInterface);
                }
                foreach (NetworkInterface adapter in adapters)
                {
                    //no fibre in our PC
                    if ((adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        || (adapter.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                        || (adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT))
                    {
                        bool find = false;
                        foreach (NetworkInterface wlnic in wlnics)
                        {
                            if (wlnic.Id == adapter.Id)
                            {
                                find = true;
                                break;
                            }
                        }
                        if (!find)
                        {
                            // wlnics.
                            nics.Add(adapter);
                        }

                    }
                }
            }
            return nics;
        }

        private bool InitAllAdapter()
        {
            List<NetworkInterface> Interfaces = GetNetworkAdapters(TYPE.ALL);
            if (Interfaces == null)
            {
                return false;
            }

            foreach (NetworkInterface Interface in Interfaces)
            {
                // to speed up
                if (Interface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                //EnableDHCPonNetworkAdapter(Interface);
            }
            return true;
        }

        public bool DisconnectWiredConnection(PhysicalAddress mac)
        {
            List<NetworkInterface> Interfaces = GetNetworkAdapters(TYPE.WIRED);
            string sFakeIP = FakeIPAddr;
            foreach (NetworkInterface Interface in Interfaces)
            {
                if (Interface.GetPhysicalAddress().ToString() == mac.ToString())
                {
                    if (Interface.OperationalStatus == OperationalStatus.Up)
                    {
                        IPv4Info IntfaceInfo = new IPv4Info();
                        IntfaceInfo.IPAddr = sFakeIP;
                        IntfaceInfo.NetMask = FakeNetMask;
                        SetIPv4InfoOnNetworkAdapters(Interface, IntfaceInfo);
                        sFakeIP = IncrIPv4(sFakeIP);
                    }
                }
            }
            Utility.MiscLib.WaitS(5);
            return true;
        }

        public bool ConnectWiredNetwork(PhysicalAddress mac)
        {
            bool bRet = false;
            List<NetworkInterface> Interfaces = GetNetworkAdapters(TYPE.WIRED);

            foreach (NetworkInterface Interface in Interfaces)
            {
                //to speed up
                if (Interface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                if (Interface.GetPhysicalAddress().ToString() != mac.ToString())
                {
                    continue;
                }
                else
                {
                    //to remove limitaion,since when configured static IP,gatway count is 0
                    //GatewayIPAddressInformationCollection gateWay = Interface.GetIPProperties().GatewayAddresses;
                    bRet = EnableDHCPonNetworkAdapter(Interface);
                    break;
                }
            }
            return bRet;
        }
        public bool RestoreWiredConnection(PhysicalAddress mac)
        {
            return ConnectWiredNetwork(mac);
        }

        public bool DisconnectWirelessNetwork()
        {
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                ClearWirelessProfile(wlanIface);
                for (int i = 0; i < 3; i++)
                {
                    if (wlanIface.DisConnectSynchronously(5000) == true)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool DisassociateWirelessNetwork(PhysicalAddress mac)
        {
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                ClearWirelessProfile(wlanIface);
                for (int i = 0; i < 3; i++)
                {
                    if (wlanIface.DisConnectSynchronously(5000) == true)
                    {
                        return true;
                    }

                }
            }
            return false;
        }
        public bool DisconnectWirelessNetwork(PhysicalAddress mac)
        {
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface == null)
            {
                return false;
            }
            ClearWirelessProfile(wlanIface);
            for (int i = 0; i < 3; i++)
            {
                if (wlanIface.DisConnectSynchronously(5000) == true)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DoWirelessConnect(WlanClient.WlanInterface wlanIface, string profileName)
        {
            if (wlanIface == null)
            {
                return false;
            }
            //wlanIface.Scan();
            //Utility.MiscLib.WaitS(5); 
            MiscLib.Debug("using adapter (" + wlanIface.InterfaceDescription + ") to connect to the router");
            Program.loginfo.Info("using adapter (" + wlanIface.InterfaceDescription + ") to connect to the router");
            Program.sw.WriteLine("using adapter (" + wlanIface.InterfaceDescription + ") to connect to the router");
            //Utility.MiscLib.WaitS(2);
            try
            {
                bool result = wlanIface.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName, ConnectionTimeOut);
                return result;
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

       

        private bool ClearWirelessProfile(WlanClient.WlanInterface wlanIface)
        {
            foreach (Wlan.WlanProfileInfo profileInfo in wlanIface.GetProfiles())
            {
                wlanIface.DeleteProfile(profileInfo.profileName);
            }
            //Utility.MiscLib.WaitS(5);
            return true;
        }
        private bool ClearWirelessProfile(PhysicalAddress mac)
        {
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface == null)
            {
                return false;
            }
            foreach (Wlan.WlanProfileInfo profileInfo in wlanIface.GetProfiles())
            {
                wlanIface.DeleteProfile(profileInfo.profileName);
            }
            Utility.MiscLib.WaitS(1);
            return true;
        }

        public bool ClearWirelessProfile()
        {
            bool bRet = true;
            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                bRet = bRet && ClearWirelessProfile(wlanIface);
            }
            return bRet;
        }

        private Wlan.WlanReasonCode SetWirelessProfile(WlanClient.WlanInterface wlanIface, string SSID)
        {

            // clean all existing profile.
            ClearWirelessProfile(wlanIface);

            string SSIDHex = Utility.MiscLib.HexEncode(SSID);
            XmlDocument xmlDoc = new XmlDocument();
            //string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", SSID, SSIDHex);
            string profileXml = "<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name></name><SSIDConfig><SSID><hex></hex><name></name></SSID><nonBroadcast>true</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>";
            xmlDoc.LoadXml(profileXml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("wl", "http://www.microsoft.com/networking/WLAN/profile/v1");
            XmlNode hexNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:hex", nsmgr);
            XmlNode textNode = xmlDoc.CreateTextNode(SSIDHex);
            hexNode.AppendChild(textNode);
            XmlNode nameTextNode1 = xmlDoc.CreateTextNode(SSID);
            XmlNode nameTextNode2 = xmlDoc.CreateTextNode(SSID);
            XmlNode rootNameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:name", nsmgr);
            XmlNode nameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:name", nsmgr);
            rootNameNode.AppendChild(nameTextNode1);
            nameNode.AppendChild(nameTextNode2);
            return wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, xmlDoc.OuterXml, true);
        }

        private Wlan.WlanReasonCode SetWirelessProfile(WlanClient.WlanInterface wlanIface, string SSID, WirelessSecurityPSK Info)
        {

            // clean all existing profile.
            ClearWirelessProfile(wlanIface);

            string SSIDHex = Utility.MiscLib.HexEncode(SSID);
            XmlDocument xmlDoc = new XmlDocument();

            string profileXml = "<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name></name><SSIDConfig><SSID><hex></hex><name></name></SSID><nonBroadcast>true</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><MSM><security><authEncryption><authentication></authentication><encryption></encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType></keyType><protected>false</protected><keyMaterial></keyMaterial></sharedKey></security></MSM></WLANProfile>";
            xmlDoc.LoadXml(profileXml);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("wl", "http://www.microsoft.com/networking/WLAN/profile/v1");
            XmlNode hexNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:hex", nsmgr);
            XmlNode textNode = xmlDoc.CreateTextNode(SSIDHex);
            hexNode.AppendChild(textNode);
            XmlNode nameTextNode1 = xmlDoc.CreateTextNode(SSID);
            XmlNode nameTextNode2 = xmlDoc.CreateTextNode(SSID);
            XmlNode rootNameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:name", nsmgr);
            XmlNode nameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:name", nsmgr);
            rootNameNode.AppendChild(nameTextNode1);
            nameNode.AppendChild(nameTextNode2);

            XmlNode typeTextNode = xmlDoc.CreateTextNode(Info.Type);
            XmlNode encryptionTextNode = xmlDoc.CreateTextNode(Info.Encryption);
            XmlNode authTextNode = xmlDoc.CreateTextNode(Info.Authentication);
            XmlNode keyTextNode = xmlDoc.CreateTextNode(Info.Key);

            MiscLib.Debug("Trying to connect router using " + Info.Authentication + ":" + Info.Encryption + ":" + Info.Key);
            Program.loginfo.Info("Trying to connect router using " + Info.Authentication + ":" + Info.Encryption + ":" + Info.Key);
            Program.sw.WriteLine("Trying to connect router using " + Info.Authentication + ":" + Info.Encryption + ":" + Info.Key);
            XmlNode authNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:authEncryption/wl:authentication", nsmgr);
            XmlNode encryptionNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:authEncryption/wl:encryption", nsmgr);
            XmlNode typeNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:sharedKey/wl:keyType", nsmgr);
            XmlNode keyNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:sharedKey/wl:keyMaterial", nsmgr);

            authNode.AppendChild(authTextNode);
            encryptionNode.AppendChild(encryptionTextNode);
            typeNode.AppendChild(typeTextNode);
            keyNode.AppendChild(keyTextNode);
            return wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, xmlDoc.OuterXml, true);
        }

        private Wlan.WlanReasonCode SetWirelessProfile(WlanClient.WlanInterface wlanIface, string SSID, WirelessSecurityEnterprise Info)
        {

            // clean all existing profile.
            ClearWirelessProfile(wlanIface);

            string SSIDHex = Utility.MiscLib.HexEncode(SSID);
            string profileXML = "<?xml version=\"1.0\"?>";
            profileXML += "<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">\n";
            profileXML += "<name></name><SSIDConfig><SSID><hex></hex><name></name></SSID><nonBroadcast>true</nonBroadcast></SSIDConfig>\n";
            profileXML += "<connectionType>ESS</connectionType><MSM><security><authEncryption><authentication></authentication>";
            profileXML += "<encryption></encryption><useOneX>true</useOneX></authEncryption>";
            profileXML += "<OneX xmlns=\"http://www.microsoft.com/networking/OneX/v1\">\n";
            profileXML += "<authMode>machineOrUser</authMode><EAPConfig><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\">";
            profileXML += "<EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type>";
            profileXML += "<VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId>";
            profileXML += "<VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType>";
            profileXML += "<AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId>";
            profileXML += "</EapMethod>";
            profileXML += "<ConfigBlob>010000003E000000010000000100000001000000150000001700000000000000000001000000170000001A00000001000000000000000000000000000000</ConfigBlob>";
            profileXML += "</EapHostConfig></EAPConfig></OneX></security></MSM></WLANProfile>";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(profileXML);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("wl", "http://www.microsoft.com/networking/WLAN/profile/v1");
            XmlNode hexNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:hex", nsmgr);
            XmlNode textNode = xmlDoc.CreateTextNode(SSIDHex);
            hexNode.AppendChild(textNode);
            XmlNode nameTextNode1 = xmlDoc.CreateTextNode(SSID);
            XmlNode nameTextNode2 = xmlDoc.CreateTextNode(SSID);
            XmlNode rootNameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:name", nsmgr);
            XmlNode nameNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:SSIDConfig/wl:SSID/wl:name", nsmgr);
            rootNameNode.AppendChild(nameTextNode1);
            nameNode.AppendChild(nameTextNode2);

            XmlNode encryptionTextNode = xmlDoc.CreateTextNode(Info.Encryption);
            XmlNode authTextNode = xmlDoc.CreateTextNode(Info.Authentication);

            XmlNode authNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:authEncryption/wl:authentication", nsmgr);
            XmlNode encryptionNode = xmlDoc.SelectSingleNode("/wl:WLANProfile/wl:MSM/wl:security/wl:authEncryption/wl:encryption", nsmgr);

            authNode.AppendChild(authTextNode);
            encryptionNode.AppendChild(encryptionTextNode);

            Wlan.WlanReasonCode result = wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, xmlDoc.OuterXml, true);
            if (result != Wlan.WlanReasonCode.Success)
            {
                return result;
            }

            profileXML = "<?xml version=\"1.0\"?>\n";
            profileXML += "<EapHostUserCredentials xmlns=\"http://www.microsoft.com/provisioning/EapHostUserCredentials\" xmlns:eapCommon=\"http://www.microsoft.com/provisioning/EapCommon\" xmlns:baseEap=\"http://www.microsoft.com/provisioning/BaseEapMethodUserCredentials\">\n";
            profileXML += "<EapMethod>\n";
            profileXML += "<Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type>\n";
            profileXML += "<VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId>\n";
            profileXML += "<VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType>\n";
            profileXML += "<AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId>\n";
            profileXML += "</EapMethod>\n";
            profileXML += "<Credentials>\n";
            profileXML += "<Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">\n";
            profileXML += "<Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">25</Type>\n";
            profileXML += "<EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\">\n";
            profileXML += "<RoutingIdentity xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\"></RoutingIdentity>\n";
            profileXML += "<Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">\n";
            profileXML += "<Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">26</Type>\n";
            profileXML += "<EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">\n";
            profileXML += "<Username xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\"></Username>\n";
            profileXML += "<Password xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\"></Password>\n";
            profileXML += "<LogonDomain xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\"></LogonDomain>\n";
            profileXML += "</EapType>\n";
            profileXML += "</Eap>\n";
            profileXML += "</EapType>\n";
            profileXML += "</Eap>\n";
            profileXML += "</Credentials>\n";
            profileXML += "</EapHostUserCredentials>\n";

            xmlDoc.LoadXml(profileXML);
            nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("credit", "http://www.microsoft.com/provisioning/EapHostUserCredentials");
            nsmgr.AddNamespace("MsChapV2", "http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1");
            nsmgr.AddNamespace("baseEap", "http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1");
            nsmgr.AddNamespace("MsPeap", "http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1");

            XmlNode usernameNode = xmlDoc.SelectSingleNode("/credit:EapHostUserCredentials/credit:Credentials/baseEap:Eap/MsPeap:EapType/baseEap:Eap/MsChapV2:EapType/MsChapV2:Username", nsmgr);
            XmlNode usernameTextNode = xmlDoc.CreateTextNode(Info.Username);
            usernameNode.AppendChild(usernameTextNode);

            XmlNode RoutingIdentityNode = xmlDoc.SelectSingleNode("/credit:EapHostUserCredentials/credit:Credentials/baseEap:Eap/MsPeap:EapType/MsPeap:RoutingIdentity", nsmgr);
            XmlNode RoutingIdentityTextNode = xmlDoc.CreateTextNode(Info.Username);
            RoutingIdentityNode.AppendChild(RoutingIdentityTextNode);

            XmlNode passwordNode = xmlDoc.SelectSingleNode("/credit:EapHostUserCredentials/credit:Credentials/baseEap:Eap/MsPeap:EapType/baseEap:Eap/MsChapV2:EapType/MsChapV2:Password", nsmgr);
            XmlNode passwordTextNode = xmlDoc.CreateTextNode(Info.Password);
            passwordNode.AppendChild(passwordTextNode);

            int EapResult = wlanIface.WlanSetProfileEapXmlUserData(SSID, xmlDoc.OuterXml);

            if (EapResult != 0)
            {
                return Wlan.WlanReasonCode.UNKNOWN;
            }
            else
            {
                return Wlan.WlanReasonCode.Success;
            }
        }

        private bool ConnectWirelessNetwork(WlanClient.WlanInterface wlanIface, string SSID)
        {
            if (SetWirelessProfile(wlanIface, SSID) != Wlan.WlanReasonCode.Success)
            {
                MiscLib.Debug("Can not save wireless profile");
                Program.loginfo.Info("Can not save wireless profile");
                Program.sw.WriteLine("Can not save wireless profile");
                return false;
            }
            try
            {
                if (DoWirelessConnect(wlanIface, SSID) == true)
                {
                    MiscLib.Debug("Successful to assicate to the router using none security ");
                    Program.loginfo.Info("Successful to assicate to the router using none security ");
                    Program.sw.WriteLine("Successful to assicate to the router using none security ");
                    MiscLib.Debug("Waiting for get IP address ...");
                    Program.loginfo.Info("Waiting for get IP address ...");
                    Program.sw.WriteLine("Waiting for get IP address ...");
                    bool result = WaitAndCheckConnection(wlanIface);
                    if (result == true)
                    {
                        MiscLib.Debug("The connection is established successfully");
                        Program.loginfo.Info("The connection is established successfully");
                        Program.sw.WriteLine("The connection is established successfully");
                        return true;
                    }
                    else
                    {
                        MiscLib.Debug("Fail to establish the connection ...");
                        Program.loginfo.Info("Fail to establish the connection ...");
                        Program.sw.WriteLine("Fail to establish the connection ...");
                        return false;
                    }
                }
                else
                {
                    MiscLib.Debug("CANNOT assicate to the router using none security ");
                    Program.loginfo.Info("CANNOT assicate to the router using none security ");
                    Program.sw.WriteLine("CANNOT assicate to the router using none security ");
                    return false;
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

        private bool AssociateWirelessNetwork(WlanClient.WlanInterface wlanIface, string SSID)
        {
            if (SetWirelessProfile(wlanIface, SSID) != Wlan.WlanReasonCode.Success)
            {
                MiscLib.Debug("Can not save wireless profile");
                Program.loginfo.Info("Can not save wireless profile");
                Program.sw.WriteLine("Can not save wireless profile");
                return false;
            }
            try
            {
                if (DoWirelessConnect(wlanIface, SSID) == true)
                {
                    MiscLib.Debug("Successful to assicate to the router using none security ");
                    Program.loginfo.Info("Successful to assicate to the router using none security ");
                    Program.sw.WriteLine("Successful to assicate to the router using none security ");
                    return true;
                }
                else
                {
                    MiscLib.Debug("CANNOT assicate to the router using none security ");
                    Program.loginfo.Info("CANNOT assicate to the router using none security ");
                    Program.sw.WriteLine("CANNOT assicate to the router using none security ");
                    return false;
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

        public bool ConnectWirelessNetwork(PhysicalAddress mac, string SSID)
        {
            bool bRet = false;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                for (int i = 0; i < CheckCount; i++)
                {
                    bRet = ConnectWirelessNetwork(wlanIface, SSID);
                    if (bRet)
                    {
                        break;
                    }
                }
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return bRet;
            }

            return bRet;
        }

        public bool AssociateWirelessNetwork(PhysicalAddress mac, string SSID)
        {
            bool bRet = false;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                bRet = AssociateWirelessNetwork(wlanIface, SSID);
                    
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                
                return bRet;
            }

            return bRet;
        }

        private bool ConnectWirelessNetwork(WlanClient.WlanInterface wlanIface, string SSID, WirelessSecurityPSK Info)
        {
            if (SetWirelessProfile(wlanIface, SSID, Info) != Wlan.WlanReasonCode.Success)
            {
                return false;
            }

            try
            {
                if (DoWirelessConnect(wlanIface, SSID) == true)
                {
                    MiscLib.Debug("Successful to associate to the router");
                    Program.loginfo.Info("Successful to associate to the router");
                    Program.sw.WriteLine("Successful to associate to the router");
                    MiscLib.Debug("Waiting for get IP address ...");
                    Program.loginfo.Info("Waiting for get IP address ...");
                    Program.sw.WriteLine("Waiting for get IP address ...");
                    bool result = WaitAndCheckConnection(wlanIface);
                    if (result == true)
                    {
                        MiscLib.Debug("The connection is established successfully");
                        Program.loginfo.Info("The connection is established successfully");
                        Program.sw.WriteLine("The connection is established successfully");
                        return true;
                    }
                    else
                    {
                        MiscLib.Debug("Fail to establish the connection ...");
                        Program.loginfo.Info("Fail to establish the connection ...");
                        Program.sw.WriteLine("Fail to establish the connection ...");
                        return false;
                    }
                }
                else
                {
                    MiscLib.Debug("CANNOT assicate to the router using none security ");
                    Program.loginfo.Info("CANNOT assicate to the router using none security ");
                    Program.sw.WriteLine("CANNOT assicate to the router using none security ");
                    return false;
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

        private bool AssociateWirelessNetwork(WlanClient.WlanInterface wlanIface, string SSID, WirelessSecurityPSK Info)
        {
            if (SetWirelessProfile(wlanIface, SSID, Info) != Wlan.WlanReasonCode.Success)
            {
                return false;
            }

            try
            {
                if (DoWirelessConnect(wlanIface, SSID) == true)
                {
                    MiscLib.Debug("Successful to associate to the router");
                    Program.loginfo.Info("Successful to associate to the router");
                    Program.sw.WriteLine("Successful to associate to the router");
                    return true;
                }
                else
                {
                    MiscLib.Debug("CANNOT assicate to the router using none security ");
                    Program.loginfo.Info("CANNOT assicate to the router using none security ");
                    Program.sw.WriteLine("CANNOT assicate to the router using none security ");
                    return false;
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

        public bool ConnectWirelessNetwork(PhysicalAddress mac, string SSID, WirelessSecurityPSK Info)
        {
            bool bRet = false;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                for (int i = 0; i < CheckCount; i++)
                {
                    bRet = ConnectWirelessNetwork(wlanIface, SSID, Info);
                    if (bRet)
                    {
                        break;
                    }
                }
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac); 
                return bRet;
            }

            return bRet;
        }

        public bool AssociateWirelessNetwork(PhysicalAddress mac, string SSID, WirelessSecurityPSK Info)
        {
            bool bRet = false;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                bRet = AssociateWirelessNetwork(wlanIface, SSID, Info);
         
            }
            else
            {
                string s = "Cannot find the interface with MAC address is " + mac;
                Utility.MiscLib.Debug(s);
                Program.loginfo.Info(s);
                Program.sw.WriteLine(s);
                return bRet;
            }

            return bRet;
        }

        private bool ConnectWirelessNetwork(WlanClient.WlanInterface wlanIface, string SSID, WirelessSecurityEnterprise Info)
        {
            if (SetWirelessProfile(wlanIface, SSID, Info) != Wlan.WlanReasonCode.Success)
            {
                return false;
            }

            try
            {
                if (DoWirelessConnect(wlanIface, SSID) == true)
                {
                    MiscLib.Debug("Successful to associate to the router");
                    Program.loginfo.Info("Successful to associate to the router");
                    Program.sw.WriteLine("Successful to associate to the router");
                    MiscLib.Debug("Waiting for get IP address ...");
                    Program.loginfo.Info("Waiting for get IP address ...");
                    Program.sw.WriteLine("Waiting for get IP address ...");
                    bool result = WaitAndCheckConnection(wlanIface);
                    if (result == true)
                    {
                        MiscLib.Debug("The connection is established successfully");
                        Program.loginfo.Info("The connection is established successfully");
                        Program.sw.WriteLine("The connection is established successfully");
                        return true;
                    }
                    else
                    {
                        MiscLib.Debug("Fail to establish the connection ...");
                        Program.loginfo.Info("Fail to establish the connection ...");
                        Program.sw.WriteLine("Fail to establish the connection ...");
                        return false;
                    }
                }
                else
                {
                    MiscLib.Debug("CANNOT assicate to the router");
                    Program.loginfo.Info("CANNOT assicate to the router");
                    Program.sw.WriteLine("CANNOT assicate to the router");
                    return false;
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
                return false;
            }
        }

        public bool ConnectWirelessNetwork(PhysicalAddress mac, string SSID, WirelessSecurityEnterprise Info)
        {
            bool bRet = false;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                for (int i = 0; i < CheckCount; i++)
                {
                    bRet = ConnectWirelessNetwork(wlanIface, SSID, Info);
                    if (bRet)
                    {
                        break;
                    }
                }
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                return bRet;
            }

            return bRet;
        }

        private bool WaitAndCheckConnection(WlanClient.WlanInterface wlanIface)
        {
            NetworkInterface Intface = wlanIface.NetworkInterface;
            bool bRet = EnableDHCPonNetworkAdapter(Intface);

            IPv4Info IntfaceInfo = null;
            //some cost down router need a long time to access,so to repeat some time
            for (int j = 0; j < CheckCount + 1; j++)
            {
                //we need refresh adapter here,other wise,will until time out
                Intface = wlanIface.NetworkInterface;

                IntfaceInfo = GetIPv4InfoFromNetworkAdapter(Intface);
                if (IntfaceInfo.Gateway != "0.0.0.0")
                {
                    break;
                }
                else
                {
                    if (j > CheckCount)
                    {
                        MiscLib.Debug("Can not get IP address via DHCP");
                        return false;
                    }
                    Utility.MiscLib.WaitS(10);
                }
            }
            //   
            bRet = false;
            for (int i = 0; i < CheckCount; i++)
            {
                bRet = CheckConnection(IntfaceInfo.Gateway);
                if (bRet)
                {
                    return bRet;
                }
            }
            return bRet;
        }

        public bool _EnableDHCP(PhysicalAddress mac)
        {
            bool bRet = false;
             WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
             if (wlanIface != null)
             {
                 NetworkInterface Intface = wlanIface.NetworkInterface;
                 bRet = EnableDHCPonNetworkAdapter(Intface);

                 IPv4Info IntfaceInfo = null;
                 //some cost down router need a long time to access,so to repeat some time
                 for (int j = 0; j < CheckCount + 1; j++)
                 {
                     //we need refresh adapter here,other wise,will until time out
                     Intface = wlanIface.NetworkInterface;

                     IntfaceInfo = GetIPv4InfoFromNetworkAdapter(Intface);
                     if (IntfaceInfo.Gateway != "0.0.0.0")
                     {
                         break;
                     }
                     else
                     {
                         if (j > CheckCount)
                         {
                             String s = "Can not get IP address via DHCP";
                             MiscLib.Debug(s);
                             Program.loginfo.Info(s);
                             Program.sw.WriteLine(s);
                             return false;
                         }
                         //Utility.MiscLib.WaitS(10);
                     }
                 }

                 bRet = CheckConnection(IntfaceInfo.Gateway);
                 if (bRet)
                 {
                     return bRet;
                 }
                 
             }
            return bRet;
        }



        private bool CheckConnection(string ip)
        {
            IPAddress ipaddr = IPAddress.Parse(ip);
            return CheckPingConnection(ipaddr);
        }

        public bool _CheckConnection(string ip)
        {
            IPAddress ResolvedIP;
            if (IPAddress.TryParse(ip, out ResolvedIP)) { return CheckPingConnection(ResolvedIP); }
            else
            {
                IPHostEntry Host = Dns.GetHostEntry(ip);
                return CheckPingConnection(Host.AddressList[0]);
            }
        }

        public int GetConnectedChannel(PhysicalAddress mac)
        {
            int iRet = -1;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.Channel;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return iRet;
            }
        }

        public int GetRSSI(PhysicalAddress mac)
        {
            int iRet = -1;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.RSSI;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return iRet;
            }
        }

        public Wlan.Dot11BssType GetBSSType(PhysicalAddress mac)
        {
            Wlan.Dot11BssType iRet = new Wlan.Dot11BssType();
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.BssType;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return iRet;
            }
        }

        public long GetTxRate(PhysicalAddress mac)
        {
            long iRet = -1;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.CurrentConnection.wlanAssociationAttributes.txRate;
            }
            else
            {
                String s = "Cannot find the interface with MAC address is " + mac;
                Utility.MiscLib.Debug(s);
                Program.loginfo.Info(s);
                Program.sw.WriteLine(s);
                return iRet;
            }
        }

        public long GetRxRate(PhysicalAddress mac)
        {
            long iRet = -1;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.CurrentConnection.wlanAssociationAttributes.rxRate;
            }
            else
            {
                String s = "Cannot find the interface with MAC address is " + mac;
                Utility.MiscLib.Debug(s);
                Program.loginfo.Info(s);
                Program.sw.WriteLine(s);
                return iRet;
            }
        }

        public uint GetSignalQuality(PhysicalAddress mac)
        {
            uint uRet = 0;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return uRet;
            }
        }
        
        public Wlan.WlanRadioState GetRadioState(PhysicalAddress mac)
        {
            Wlan.WlanRadioState a = new Wlan.WlanRadioState();
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.RadioState;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return a;
            }
        }

        public Wlan.WlanAuthCipherPairList GetSupportedInfraAuthCipherPairs(PhysicalAddress mac)
        {
            Wlan.WlanAuthCipherPairList a = new Wlan.WlanAuthCipherPairList();
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.SupportedInfraAuthCipherPairs;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return a;
            }
        }

        public Wlan.WlanAuthCipherPairList GetSupportedAdhocAuthCipherPairs(PhysicalAddress mac)
        {
            Wlan.WlanAuthCipherPairList a = new Wlan.WlanAuthCipherPairList();
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                return wlanIface.SupportedAdhocAuthCipherPairs;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return a;
            }
        }

        public StringBuilder GetAvailableNetworkList(PhysicalAddress mac)
        {
            StringBuilder SSIDList = new StringBuilder();
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface != null)
            {
                Wlan.WlanAvailableNetwork[] a = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in a)
                {
                    string ssid = GetStringForSSID(network.dot11Ssid);
                    if (ssid != "")
                    {
                        SSIDList.AppendLine(ssid);
                    }
                    else
                    {
                        int i = 0;
                    }
                }
                return SSIDList;
            }
            else
            {
                Utility.MiscLib.Debug("Cannot find the interface with MAC address is " + mac);
                Program.loginfo.Info("Cannot find the interface with MAC address is " + mac);
                Program.sw.WriteLine("Cannot find the interface with MAC address is " + mac);
                return SSIDList;
            }
        }

        /*
        public bool ResetDevice(string sDev)
        {
            HH_Lib hwh = new HH_Lib();
            bool bRet = false;
            MiscLib.Debug("Trying to find device (" + sDev + ")");
            string[] HardwareList = hwh.GetAll();
            foreach (string s in HardwareList)
            {
                int iIndex = sDev.LastIndexOf("#");
                string sDesc = sDev.Remove(iIndex);
                if (s.Trim() == sDesc.Trim())
                {
                    MiscLib.Debug("Device (" + sDev + ") found!");
                    MiscLib.Debug("Trying to disable device (" + sDev + ")");
                    bRet = hwh.SetDeviceState(s, false);
                    if (!bRet)
                    {
                        MiscLib.Debug("Fail to disable device (" + sDev + ")");
                        return bRet;
                    }
                    MiscLib.Debug("Disable device (" + sDev + ") done");

                    //if can not enabled,will cause many case can not be run,so try to enable 
                    //for serval time if enable fail.
                    for (int i = 0; i < 3; i++)
                    {
                        Utility.MiscLib.WaitS(5);
                        MiscLib.Debug("Trying to enable device (" + sDev + ")");
                        bRet = hwh.SetDeviceState(s, true);
                        if (!bRet)
                        {
                            MiscLib.Debug("Fail to enable device (" + sDev + ")");
                            continue;
                        }
                        else
                        {
                            MiscLib.Debug("Enable device (" + sDev + ") done");
                            break;
                        }
                    }
                    break;
                }
            }

            Utility.MiscLib.WaitS(5);
            return bRet;
        }
        */
        public bool IsSSIDScaned(string SSID)
        {
            if ((WirelessClient.Interfaces == null) || (WirelessClient.Interfaces.Length <= 0))
            {
                return false;
            }

            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                // clean all existing profile.
                ClearWirelessProfile(wlanIface);
                wlanIface.Scan();
                Utility.MiscLib.WaitS(5);
                //scan for 3 min,Some DUT is very slow
                for (int i = 1; i <= 36; i++)
                {
                    wlanIface.Scan();
                    Utility.MiscLib.WaitS(5);
                    Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                    foreach (Wlan.WlanAvailableNetwork network in networks)
                    {
                        string SSID_Scaned = GetStringForSSID(network.dot11Ssid);
                        if (SSID_Scaned == SSID)
                        {
                            MiscLib.Debug("SSID (" + SSID + ") is scanned");
                            Program.loginfo.Info("SSID (" + SSID + ") is scanned");
                            Program.sw.WriteLine("SSID (" + SSID + ") is scanned");
                            //Wlan.Dot11AuthAlgorithm auth = network.dot11DefaultAuthAlgorithm;
                            return true;
                        }
                    }
                    MiscLib.Debug("SSID (" + SSID + ") can not be found");
                    Program.loginfo.Info("SSID (" + SSID + ") can not be found");
                    Program.sw.WriteLine("SSID (" + SSID + ") can not be found");
                }
            }
            return false;
        }

        public bool IsSSIDScaned(string SSID, bool reset)
        {
            if ((WirelessClient.Interfaces == null) || (WirelessClient.Interfaces.Length <= 0))
            {
                return false;
            }

            foreach (WlanClient.WlanInterface wlanIface in WirelessClient.Interfaces)
            {
                // clean all existing profile.
                ClearWirelessProfile(wlanIface);

                //need do more research here
                /*
                if (reset)
                {
                    bool bRet = ResetDevice(wlanIface.InterfaceDescription);
                    if (!bRet)
                    {
                        Console.WriteLine("ERROR : " + DateTime.Now.ToString() + " : " + "Can not enable/disable " + wlanIface.InterfaceDescription);
                        return false;
                    }
                }
                */
                MiscLib.Debug("Waiting for 5s");
                Program.loginfo.Info("Waiting for 5s");
                Program.sw.WriteLine("Waiting for 5s");
                Utility.MiscLib.WaitS(5);
                //scan for 5 min,Some DUT is very slow
                for (int i = 1; i <= 30; i++)
                {
                    MiscLib.Debug("Try to scan SSID " + SSID);
                    Program.loginfo.Info("Try to scan SSID " + SSID);
                    Program.sw.WriteLine("Try to scan SSID " + SSID);
                    wlanIface.Scan();
                    Utility.MiscLib.WaitS(5);
                    Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                    foreach (Wlan.WlanAvailableNetwork network in networks)
                    {
                        string SSID_Scaned = GetStringForSSID(network.dot11Ssid);
                        //Console.WriteLine("Scanned SSID : " + SSID_Scaned);
                        if (SSID_Scaned == SSID)
                        {
                            MiscLib.Debug("SSID (" + SSID + ") is scanned");
                            Program.loginfo.Info("SSID (" + SSID + ") is scanned");
                            Program.sw.WriteLine("SSID (" + SSID + ") is scanned");
                            //Wlan.Dot11AuthAlgorithm auth = network.dot11DefaultAuthAlgorithm;
                            return true;
                        }
                    }
                    MiscLib.Debug("SSID (" + SSID + ") can not be found");
                    Program.loginfo.Info("SSID (" + SSID + ") can not be found");
                    Program.sw.WriteLine("SSID (" + SSID + ") can not be found");
                }
            }

            return false;
        }

        public string GetBSSID(PhysicalAddress mac, string sSSID)
        {
            string sBSSID = null;
            WlanClient.WlanInterface wlanIface = FoundWlanIf(mac);
            if (wlanIface == null)
            {
                return null;
            }

            Wlan.WlanBssEntry[] bssidList = wlanIface.GetNetworkBssList();
            if (bssidList == null || bssidList.Length <= 0)
            {
                return sBSSID;
            }
            foreach (Wlan.WlanBssEntry bssidEntry in bssidList)
            {
                if (GetStringForSSID(bssidEntry.dot11Ssid) == sSSID)
                {
                    byte[] bytes = bssidEntry.dot11Bssid;
                    string wMac = null;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        wMac += bytes[i].ToString("X2");
                        if (i != bytes.Length - 1)
                        {
                            wMac += ":";
                        }
                    }
                    sBSSID = wMac;
                }
            }
            return sBSSID;
        }

        public string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        public string[] GetMACList()
        {
            List<string> lMac = new List<string>();
            string[] arMac = null;
            try
            {
                int index = 0;
                NetworkInterface[] niAllNIC = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in niAllNIC)
                {
                    PhysicalAddress pAddr1 = ni.GetPhysicalAddress();
                    string sMAC1 = ConvertPhyAddrToMac(pAddr1);

                    if ((ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet) &&
                        (ni.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet) &&
                        (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                        )
                    {
                        continue;
                    }
                    PhysicalAddress pAddr = ni.GetPhysicalAddress();
                    string sMAC = ConvertPhyAddrToMac(pAddr);
                    lMac.Add(sMAC);
                }
                arMac = new string[lMac.Count];
                foreach (string mac in lMac)
                {
                    arMac[index] = mac;
                    index++;
                }
            }
            catch //(Exception ex)
            {
                return arMac;
            }
            return arMac;
        }
        public string GetRandomMAC()
        {
            string dev = "";
            String[] seed = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            Random randObj = new Random();
            int start = 1;
            int end = 16;
            string mac = "00:";
            for (int i = 0; i < 5; i++)
            {
                mac += seed[randObj.Next(start, end)] + seed[randObj.Next(start, end)];
                if (i < 4)
                {
                    mac += ":";
                }
            }
            dev = mac;
            return dev;
        }

        public Device[] GetDeviceList()
        {
            Device[] device = null;
            try
            {
                int index = 0;
                string[] MacList = GetMACList();
                device = new Device[MacList.Length];
                foreach (string mac in MacList)
                {
                    device[index].MacAddress = mac;
                    device[index].IPAddress = "";
                    device[index].IPStart = "";
                    device[index].IPEnd = "";
                    index++;
                }
            }
            catch //(Exception ex)
            {
                return device;
            }
            return device;
        }

        public Device GetRandomDevice()
        {
            Device dev = new Device();
            String[] seed = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            Random randObj = new Random();
            int start = 1;
            int end = 16;
            string mac = "00:";
            for (int i = 0; i < 5; i++)
            {
                mac += seed[randObj.Next(start, end)] + seed[randObj.Next(start, end)];
                if (i < 4)
                {
                    mac += ":";
                }
            }
            dev.MacAddress = mac;
            dev.IPAddress = "";
            dev.IPStart = "";
            dev.IPEnd = "";
            return dev;
        }

        public bool CheckSocketConnection(IPAddress ipaddr, int port, ProtocolType protocol)
        {
            if (ipaddr == null || port == 0)
            {
                return false;
            }

            IPEndPoint remoteEP = new IPEndPoint(ipaddr, port);
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, protocol);

            MiscLib.Debug("Trying to connect to " + ipaddr + " via port " + port);
            try
            {
                sender.Connect(remoteEP);
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                MiscLib.Debug("Succeed in connecting to " + ipaddr + " via port " + port);
                return true;
            }
            catch (Exception ex)
            {
                MiscLib.Debug(ex.Message);
                MiscLib.Debug("Fail to connect to " + ipaddr + " via port " + port);
                return false;
            }
        }

        public string CheckHttpConnection(string NCIndex, string url)
        {
            List<NetworkInterface> sWirelessList = GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
            PhysicalAddress mac = sWirelessList[Convert.ToInt16(NCIndex)].GetPhysicalAddress();
            
            NetworkInterface NI = GetNetworkInferface(mac);
            IPv4Info IntfaceInfo = GetIPv4InfoFromNetworkAdapter(NI);
            string ip = IntfaceInfo.IPAddr;
            if (!url.StartsWith("http://")) { url = "http://" + url; }
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.ServicePoint.BindIPEndPointDelegate = delegate
            {
                return new IPEndPoint(IPAddress.Parse(ip), 0);
            };
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = null;
            StreamReader reader = null;
            string responseFromServer = null;

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            reader = new StreamReader(dataStream);
            // Read the content.
            responseFromServer = reader.ReadToEnd();
            // Cleanup the streams and the response.
            return responseFromServer;
        }

        public string GetFTPFile(string NCIndex, string url, string username , string password)
        {
            List<NetworkInterface> sWirelessList = GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
            PhysicalAddress mac = sWirelessList[Convert.ToInt16(NCIndex)].GetPhysicalAddress();

            NetworkInterface NI = GetNetworkInferface(mac);
            IPv4Info IntfaceInfo = GetIPv4InfoFromNetworkAdapter(NI);
            string ip = IntfaceInfo.IPAddr;

            string dFolder = AppDomain.CurrentDomain.BaseDirectory + @"Download";
            
            if (!Directory.Exists(dFolder)) {Directory.CreateDirectory(dFolder); }
            string dfn = Path.GetFileName(url);
            if (!url.StartsWith("ftp://")) { url = "ftp://" + url; }
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(username, password);

            request.ServicePoint.BindIPEndPointDelegate = delegate
            {
                return new IPEndPoint(IPAddress.Parse(ip), 0);
            };
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            using (StreamWriter destination = new StreamWriter(dFolder + @"\" + dfn))
            {
                destination.Write(reader.ReadToEnd());
                destination.Flush();
            }

            //Console.WriteLine("Download Complete, status {0}", response.StatusDescription);

            reader.Close();
            response.Close();
            return response.StatusDescription;
        }

        public bool CheckPingConnection(IPAddress ipaddr)
        {
            if (ipaddr == null)
            {
                return false;
            }

            Ping sender = new Ping();
            
            bool FinalRet = false;
            try
            {
                for (int i = 1; i <= 2; i++)
                {
                    String s = "Try to ping " + ipaddr + " " + i + " time.";
                    MiscLib.Debug(s);
                    Program.loginfo.Info(s);
                    Program.sw.WriteLine(s);
                    PingReply reply = sender.Send(ipaddr,2000);

                    if (reply.Status == IPStatus.Success)
                    {
                        s = "The echo reply from " + ipaddr + " is got successfully.";
                        MiscLib.Debug(s);
                        Program.loginfo.Info(s);
                        Program.sw.WriteLine(s);
                        FinalRet = true;
                        //chxiao,speed up
                        return FinalRet;
                    }
                    else
                    {
                        s = "The echo reply from " + ipaddr + " is not got successfully.";
                        MiscLib.Debug(s);
                        Program.loginfo.Info(s);
                        Program.sw.WriteLine(s);
                        s = "The status is " + reply.Status;
                        MiscLib.Debug(s);
                        Program.loginfo.Info(s);
                        Program.sw.WriteLine(s);
                        //Utility.MiscLib.WaitS(2);
                    }
                }
            }
            catch (Exception e)
            {
                MiscLib.Debug(e.Message);
                Program.loginfo.Info(e.Message);
                Program.sw.WriteLine(e.Message);
            }
            return FinalRet;
        }

        public void FlushDNS()
        {
            MiscLib.Debug("Flush the DNS cache on the machine.");
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine("ipconfig /flushdns");
                p.StandardInput.WriteLine("exit");
                MiscLib.Debug("Waiting to flush dns cache");
                Utility.MiscLib.WaitS(NICSettingEffectTime);
            }
            catch (Exception ex)
            {
                MiscLib.Debug(ex.Message);
                MiscLib.Debug("DNS is blocked by Access Schdule");
            }
        }

        private string IncrIPv4(string ip)
        {
            long[] oldIP = new long[4];
            string[] s = ip.Split('.');
            oldIP[0] = long.Parse(s[0]);
            oldIP[1] = long.Parse(s[1]);
            oldIP[2] = long.Parse(s[2]);
            oldIP[3] = long.Parse(s[3]) + 1;
            //network order,host order
            //long newIP = (oldIP[0] << 24) + (oldIP[1] << 16) + (oldIP[2] << 8) + oldIP[3];
            long newIP = (oldIP[3] << 24) + (oldIP[2] << 16) + (oldIP[1] << 8) + oldIP[0];
            IPAddress p = new IPAddress(newIP);
            return p.ToString();
        }

        public string ConvertPhyAddrToMac(PhysicalAddress pAddr)
        {

            byte[] bytes = pAddr.GetAddressBytes();
            string wMac = null;
            for (int i = 0; i < bytes.Length; i++)
            {
                wMac += bytes[i].ToString("X2");
                if (i != bytes.Length - 1)
                {
                    wMac += ":";
                }
            }
            return wMac;
        }

        public PhysicalAddress ConvertMacToPhyAddr(string sMAC)
        {

            PhysicalAddress mac = PhysicalAddress.Parse(sMAC);
            return mac;
        }

    }
}
