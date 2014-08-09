using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using Utility;

using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using log4net;
using System.Text.RegularExpressions;

namespace WifiAPI
{
    class Program
    {
        static string hostname = Dns.GetHostName();
        static IPAddress[] addrs = Dns.Resolve(hostname).AddressList;
        static TcpListener wTcpListener = wireTcpListener();
        /*
        static DateTime now = DateTime.Now;
        static string dateForm = @"yyyyMMddHHmmss";
        static string datenow = now.ToString(dateForm);
        static StreamWriter logfile = new StreamWriter("c:\\WifiAPI_"+ datenow +".log", true);
        */
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        public static TcpListener wireTcpListener()
        {
            TcpListener listener = null;
            List<NetworkInterface> sWirelessList = ToolSettings.NC.GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
            foreach (IPAddress IP in addrs)
            {
                bool IPforWireless = false;
                foreach (NetworkInterface NC in sWirelessList)
                {
                    if (ToolSettings.NC.GetIPv4InfoFromNetworkAdapter(NC).IPAddr == IP.ToString())
                    {
                        IPforWireless = true;
                    }
                }
                if (IPforWireless) { continue; }
                listener = new TcpListener(IP, 2014);
                
            }
            return listener;
            
        }
        public static StreamWriter sw = null;
        public static void Service()
        {
            List<NetworkInterface> sWirelessList = ToolSettings.NC.GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
                
            while (true)
            {
                Socket soc = wTcpListener.AcceptSocket();
                //soc.SetSocketOption(SocketOptionLevel.Socket,
                //        SocketOptionName.ReceiveTimeout,10000);

                Console.WriteLine("Connected: {0}", soc.RemoteEndPoint);
                loginfo.Info("Connected: " + soc.RemoteEndPoint);
                
                try
                {
                    Stream s = new NetworkStream(soc);
                    StreamReader sr = new StreamReader(s);
                    sw = new StreamWriter(s);
                    sw.AutoFlush = true; // enable automatic flushing
                    string prompt = "";
                    sWirelessList = ToolSettings.NC.GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
                    if (sWirelessList.Count != 0) 
                    {
                        IPv4Info IntfaceInfo = ToolSettings.NC.GetIPv4InfoFromNetworkAdapter(sWirelessList[0]);
                        prompt = sWirelessList[0].Description+"_<WIFIHive>#: ";
                    }
                    
                    sw.Write(prompt);
                    while (true)
                    {
                        string cmd = sr.ReadLine();
                        if (cmd == "exit") break;
                        loginfo.Info(cmd);
                        string d = "" ;
                        StringBuilder db = new StringBuilder();
                        Regex splitter = new Regex(@"\s+");
                        string[] arg = splitter.Split(cmd);
                        switch (arg[0])
                        {
                            
                            case "help":
                                d = @"help       - display supported command
listnc      - list wifi cards on this server
listnt      - list available wireless network for a wifi cards
associate   - associate with specified wireless network
disasso     - disassociate wireless on sepcified wifi card
dhcp        - enable dhcp on specified wifi card(after association)
getrate     - get wifi interface Tx/Rx rate
ping        - check connection with ping
httpget     - http get method for a specified URL
ftpget      - ftp get a file
exit        - exit login
";
                                break;
                            case "listnc":
                                db = new StringBuilder();
                                sWirelessList = ToolSettings.NC.GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
                                if (sWirelessList.Count == 0) { d = "No Wireless card available!"; break; }
                                for (int i = 0; i < sWirelessList.Count; i ++)
                                {
                                    IPv4Info IntfaceInfo = ToolSettings.NC.GetIPv4InfoFromNetworkAdapter(sWirelessList[i]);
                                    db.AppendLine("Index: " + i.ToString() + "\tDescr: " + sWirelessList[i].Description + "\t\tOPState: " + sWirelessList[i].OperationalStatus.ToString() + "\tMAC: " + sWirelessList[i].GetPhysicalAddress().ToString() + "\tIP: " + IntfaceInfo.IPAddr + "\tGateway: " + IntfaceInfo.Gateway);
                                }
                                d = db.ToString();
                                break;
                            case "listnt":
                                db = null;
                                if (arg.Length < 2) { d = "Usage: listnt <NCindex>"; break; }
                                try
                                {
                                    db = ToolSettings.NC.GetAvailableNetworkList(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress());
                                }
                                catch(Exception e)
                                {
                                    d = e.ToString(); break;
                                }
                                d = db.ToString();
                                break;
                            case "associate":
                                try
                                {
                                    if (arg.Length != 3 && arg.Length < 6) { d = @"associate <NCIndex> <ssid> [authentication] [encryption] [key] [type]"; break; }
                                    if (arg.Length > 3)
                                    {
                                        WirelessSecurityPSK psk = new WirelessSecurityPSK();
                                        psk.Authentication = arg[3];
                                        psk.Encryption = arg[4];
                                        psk.Key = arg[5];
                                        psk.Type = (arg.Length > 6) ? arg[6] : "passPhrase";
                                        d = (ToolSettings.NC.AssociateWirelessNetwork(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress(), arg[2], psk)) ? ">>>>>>>SUCCESS" : ">>>>>>>FAIL";

                                    }
                                    else
                                    {
                                        d = (ToolSettings.NC.AssociateWirelessNetwork(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress(), arg[2]))?">>>>>>>SUCCESS":">>>>>>>FAIL";

                                    }
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "disasso":
                                try
                                {
                                    if (arg.Length != 2)
                                    {
                                        d = "Usage: disasso <NCIndex>";
                                        break;
                                    }
                                    d = (ToolSettings.NC.DisassociateWirelessNetwork(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress())) ? ">>>>>>>SUCCESS" : ">>>>>>>FAIL";
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "dhcp":
                                try
                                {
                                    if (arg.Length != 2)
                                    {
                                        d = "Usage: dhcp <NCIndex>";
                                        break;
                                    }
                                    sWirelessList = ToolSettings.NC.GetNetworkAdapters(NetworkConnection.TYPE.WIRELESS);
                                    IPv4Info IntfaceInfo = ToolSettings.NC.GetIPv4InfoFromNetworkAdapter(sWirelessList[Convert.ToInt16(arg[1])]);
                                    d = (ToolSettings.NC._EnableDHCP(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress())) ? ">>>>>>>SUCCESS" + "\tIP: " + IntfaceInfo.IPAddr + "\tGateway: " + IntfaceInfo.Gateway : ">>>>>>>FAIL";
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "getrate":
                                try
                                {
                                    if (arg.Length != 2)
                                    {
                                        d = "Usage: getrate <NCIndex>";
                                        break;
                                    }
                                    d = "TxRate: " + ToolSettings.NC.GetTxRate(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress()).ToString() + "; RxRate:" + ToolSettings.NC.GetRxRate(sWirelessList[Convert.ToInt16(arg[1])].GetPhysicalAddress()).ToString();
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "ping":
                                try
                                {
                                    if (arg.Length != 2)
                                    {
                                        d = "Usage: ping <IPAddr>";
                                        break;
                                    }
                                    d = ToolSettings.NC._CheckConnection(arg[1]) ? ">>>>>>>SUCCESS" : ">>>>>>>FAIL";
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "httpget":
                                try
                                {
                                    if (arg.Length != 3)
                                    {
                                        d = "Usage: httpget <NCIndex> <URL>";
                                        break;
                                    }
                                    d = ToolSettings.NC.CheckHttpConnection(arg[1], arg[2]);
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "ftpget":
                                try
                                {
                                    if (arg.Length != 6)
                                    {
                                        d = "Usage: ftpget <NCIndex> <ftpserver> <username> <password> <filetoget>";
                                        break;
                                    }
                                    d = ToolSettings.NC.GetFTPFile(arg[1], arg[2] +"/" + arg[5] ,arg[3],arg[4] );
                                    break;
                                }
                                catch (Exception e)
                                {
                                    d = e.ToString();
                                    loginfo.Info(e.ToString());
                                    break;
                                }
                            case "":
                                break;
                            default:
                                d = "unsupported command! please retry:";
                                break;
                        }
                            d =  d +"\n"+ prompt;
                            sw.Write(d);
                     }
                        
                            
                            
                            //WirelessSecurityPSK psk = new WirelessSecurityPSK();
                            //psk.Authentication = "WPAPSK";
                            //psk.Encryption = "AES";
                            //psk.Key = "11111111";
                            //psk.Type = "passPhrase";
                            //ToolSettings.NC.ConnectWirelessNetwork(SuiteParameterSettings.WLPrimaryMAC, "qqqq", psk);
                             
                        
                        //sw.WriteLine(name);
                    
                    s.Close();
                }
                catch (Exception e)
                {

                    Console.WriteLine(e.Message);
                    loginfo.Info(e.Message);
                }

                Console.WriteLine("Disconnected: {0}", soc.RemoteEndPoint);
                loginfo.Info("Disconnected: " + soc.RemoteEndPoint);

                soc.Close();
            }
        }
        static void Main(string[] args)
        {


            wTcpListener.Start();
            
            Console.WriteLine("Server mounted, listening to port 2014");
            loginfo.Info("Server mounted, listening to port 2014");
            for (int i = 0; i < 100; i++)
            {
                Thread t = new Thread(new ThreadStart(Service));
                t.Start();
            }
            /*
            WirelessSecurityPSK psk = new WirelessSecurityPSK();
            psk.Authentication = "WPAPSK";
            psk.Encryption = "AES";
            psk.Key = "11111111";
            psk.Type = "passPhrase";
            
            ToolSettings.NC.ConnectWirelessNetwork(SuiteParameterSettings.WLPrimaryMAC, "qqqq", psk);
            
            MiscLib.Debug("BSS Type: " + ToolSettings.NC.GetBSSType(SuiteParameterSettings.WLPrimaryMAC).ToString());
            MiscLib.Debug("SupportedInfraAuthCipherPairs: " + ToolSettings.NC.GetSupportedInfraAuthCipherPairs(SuiteParameterSettings.WLPrimaryMAC).ToString());
            MiscLib.Debug("SupportedAdhocAuthCipherPairs: " + ToolSettings.NC.GetSupportedAdhocAuthCipherPairs(SuiteParameterSettings.WLPrimaryMAC).ToString());
            MiscLib.Debug("Connected channel: " + ToolSettings.NC.GetConnectedChannel(SuiteParameterSettings.WLPrimaryMAC));
            MiscLib.Debug("BSSID: " + ToolSettings.NC.GetBSSID((SuiteParameterSettings.WLPrimaryMAC), "qqqq"));
            MiscLib.Debug("RSSI: " + ToolSettings.NC.GetRSSI(SuiteParameterSettings.WLPrimaryMAC).ToString());
            MiscLib.Debug("Tx Rate: " + ToolSettings.NC.GetTxRate(SuiteParameterSettings.WLPrimaryMAC).ToString());
            MiscLib.Debug("Signal Quality: " + ToolSettings.NC.GetSignalQuality(SuiteParameterSettings.WLPrimaryMAC).ToString());
            
            Utility.MiscLib.WaitS(5);
             */
        }
        

    }
    
}

