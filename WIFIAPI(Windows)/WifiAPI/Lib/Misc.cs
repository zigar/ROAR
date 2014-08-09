

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Threading;
//using TimeSync;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
//using HNAP;
namespace Utility
{
    class MiscLib
    {
        static string[] ROUTER_NTP_IP = new string[] { "209.81.9.7", "132.163.4.102" };
        static string LAN_KEY_LOCATION = @"Software\Microsoft\Internet Explorer\International";

        // SYSTEMTIME structure used by SetSystemTime
        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public short year;
            public short month;
            public short dayOfWeek;
            public short day;
            public short hour;
            public short minute;
            public short second;
            public short milliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TimeZoneInformation
        {

            public int bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string standardName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string daylightName;
            SYSTEMTIME standardDate;
            SYSTEMTIME daylightDate;
            public int standardBias;
            public int daylightBias;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetTimeZoneInformation(out TimeZoneInformation lpTimeZoneInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetTimeZoneInformation(ref TimeZoneInformation lpTimeZoneInformation);

        [DllImport("kernel32.dll")]
        static extern bool SetLocalTime(ref SYSTEMTIME time);

        public static string HexEncode(string strEncode)
        {
            string strReturn = "";
            foreach (short shortx in strEncode.ToCharArray())
            {
                strReturn += shortx.ToString("X2");
            }
            return strReturn;
        }

        public static string HexDecode(string strDecode)
        {
            string sResult = "";
            for (int i = 0; i < strDecode.Length / 2; i++)
            {
                sResult += (char)short.Parse(strDecode.Substring(i * 2, 2), global::System.Globalization.NumberStyles.HexNumber);
            }
            return sResult;
        }
        /*
        public static DateTime GetNTPUTCTime(string[] ntp)
        {
            DateTime DT = DateTime.MinValue.ToUniversalTime();
            try
            {
                foreach (string ip in ntp)
                {
                    NTPClient nc = new NTPClient(ip);
                    DT = nc.GetNTPTime().ToUniversalTime();
                    if (DT != DateTime.MinValue.ToUniversalTime())
                    {
                        return DT;
                    }
                }
            }
            catch (Exception e)
            {
                Debug(e.Message);
            }
            return DT;
        }
        
        public static DateTime GetNTPUTCTime()
        {
            return GetNTPUTCTime(ROUTER_NTP_IP);
        }
        */
        public static bool SetLocalTime(DateTime DT)
        {
            SYSTEMTIME st;

            DateTime trts = DT;
            st.year = (short)trts.Year;
            st.month = (short)trts.Month;
            st.dayOfWeek = (short)trts.DayOfWeek;
            st.day = (short)trts.Day;
            st.hour = (short)trts.Hour;
            st.minute = (short)trts.Minute;
            st.second = (short)trts.Second;
            st.milliseconds = (short)trts.Millisecond;

            return SetLocalTime(ref st);
        }

        public static bool SetTimeZone(TimeZoneInformation tzi)
        {
            return SetTimeZoneInformation(ref tzi);
        }


        public static TimeZoneInformation GetTimeZone()
        {
            TimeZoneInformation tzi;
            int currentTimeZone = GetTimeZoneInformation(out tzi);
            return tzi;
        }

        /*
        public static bool SetAcceptLanguage(string language)
        {
            try
            {
                RegistryKey root = Registry.CurrentUser.OpenSubKey(LAN_KEY_LOCATION, RegistryKeyPermissionCheck.ReadWriteSubTree);
                //string sLan = language + ";q=0.5";
                string sLan = language;
                root.SetValue("AcceptLanguage", sLan);
                root.Close();
                //waiting to take effect
                Utility.MiscLib.WaitS(2);
                return true;
            }
            catch (Exception ex)
            {
                MiscLib.Debug(ex.Message);
                return false;
            }

        }
        */
        public static void Debug(string sInfo)
        {
            Console.WriteLine("DEBUG : : " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + sInfo);
        }
        /*
        public static bool CloseAllIE()
        {
            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcesses();

            foreach (System.Diagnostics.Process myProcess in myProcesses)
            {
                if (myProcess.ProcessName.ToUpper() == "IEXPLORE")
                {
                    myProcess.Kill();
                }
            }
            Utility.MiscLib.WaitS(1);
            return true;
        }
        */
        //must with http://
        public static void SkipAlert(string URL, string UserName, string Password)
        {
            HttpWebRequest oRequest;
            CookieContainer oCookie = new CookieContainer();
            try
            {
                oRequest = (HttpWebRequest)WebRequest.Create(URL);
            }
            catch
            {
                return;
            }

            byte[] authBytes = Encoding.UTF8.GetBytes(UserName + ":" + Password);
            oCookie = new CookieContainer();
            ASCIIEncoding encoding = new ASCIIEncoding();
            oRequest.Method = "GET";
            oRequest.KeepAlive = false;
            oRequest.Accept = @"image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/xaml+xml, application/vnd.ms-xpsdocument, application/x-ms-xbap, application/x-ms-application, application/x-silverlight, */*";
            //oRequest.Headers.Add("Accept-Language: zh-cn");
            oRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
            oRequest.Headers.Add("Accept-Encoding: gzip, deflate");
            oRequest.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
            oRequest.CookieContainer = oCookie;
            WebResponse oResponse = null;
            try
            {
                oResponse = oRequest.GetResponse();
            }
            catch //(WebException e)
            {
                return;
            }
            oResponse.Close();
        }
        /*
        public static void DisableWLBlockingPage(DUT_TYPE dut)
        {
            if (dut == DUT_TYPE.VIPER)
            {
                skipViperWLAlert();
            }
            else
            {
                skipCESWLAlert();
            }
        }
        */
        public static void DisableWLBlockingPage()
        {
            skipCESWLAlert();
            skipViperWLAlert();
        }

        public static void skipCESWLAlert()
        {
            string sURL = "http://192.168.1.1:52000/Unsecured.cgi?AdminUI_Show=0";
            HttpWebRequest oRequest;
            CookieContainer oCookie = new CookieContainer();
            try
            {
                oRequest = (HttpWebRequest)WebRequest.Create(sURL);
            }
            catch
            {
                return;
            }

            //byte[] authBytes = Encoding.UTF8.GetBytes(UserName + ":" + Password);
            oCookie = new CookieContainer();
            ASCIIEncoding encoding = new ASCIIEncoding();
            oRequest.Method = "GET";
            oRequest.KeepAlive = false;
            oRequest.Accept = @"image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/xaml+xml, application/vnd.ms-xpsdocument, application/x-ms-xbap, application/x-ms-application, application/x-silverlight, */*";
            //oRequest.Headers.Add("Accept-Language: zh-cn");
            //oRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
            oRequest.Headers.Add("Accept-Encoding: gzip, deflate");
            oRequest.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
            oRequest.CookieContainer = oCookie;
            WebResponse oResponse = null;
            try
            {
                oResponse = oRequest.GetResponse();
            }
            catch (Exception e)
            {
                Utility.MiscLib.Debug(e.Message);
                return;
            }
            oResponse.Close();
        }

        public static void skipViperWLAlert()
        {
            string sRootURL = @"http://192.168.1.1:52000";
            string sURL = sRootURL + @"/apply.cgi";
            HttpWebRequest oRequest;
            CookieContainer oCookie = new CookieContainer();
            try
            {
                oRequest = (HttpWebRequest)WebRequest.Create(sURL);
            }
            catch
            {
                return;
            }

            oCookie = new CookieContainer();
            ASCIIEncoding encoding = new ASCIIEncoding();
            oRequest.Method = "POST";
            oRequest.KeepAlive = false;
            oRequest.Accept = @"image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/xaml+xml, application/vnd.ms-xpsdocument, application/x-ms-xbap, application/x-ms-application, application/x-silverlight, */*";
            oRequest.Headers.Add("Accept-Language: zh-cn");
            //oRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(authBytes);
            oRequest.Headers.Add("Accept-Encoding: gzip, deflate");
            oRequest.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
            oRequest.CookieContainer = oCookie;
            string postData = @"submit_button=UnsecuredEnable&change_action=&action=Apply&next_url=&wait_time=19&submit_type=&next_page=http%3A%2F%2F192.168.1.1%2Findex.asp";
            byte[] data = encoding.GetBytes(postData);
            oRequest.Referer = sRootURL + @"/UnsecuredEnable.asp?target=192.168.1.1/index.asp";
            oRequest.ContentType = "application/x-www-form-urlencoded";
            oRequest.ContentLength = data.Length;

            Stream newStream = null;
            WebResponse oResponse = null;
            oRequest.Timeout = 30000;
            try
            {
                newStream = oRequest.GetRequestStream();
                // Send the data.
                newStream.Write(data, 0, data.Length);
                oResponse = oRequest.GetResponse();
            }
            catch (Exception e)
            {
                Utility.MiscLib.Debug(e.Message);
                if (newStream != null)
                {
                    newStream.Close();
                }
                return;
            }
            oResponse.Close();
            newStream.Close();
        }

        public static string GetAcceptLanguage()
        {
            try
            {
                RegistryKey root = Registry.CurrentUser.OpenSubKey(LAN_KEY_LOCATION, RegistryKeyPermissionCheck.ReadWriteSubTree);
                object o = root.GetValue("AcceptLanguage", "en");
                root.Close();
                string s = (string)o;
                return s;
            }
            catch
            {
                return "en";
            }
        }

        public static bool SetAccountAccpetable()
        {
            string sKey = @"software\Microsoft\Internet Explorer\Main\FeatureControl";
            try
            {
                RegistryKey root = Registry.CurrentUser.CreateSubKey(sKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey key = root.CreateSubKey("FEATURE_HTTP_USERNAME_PASSWORD_DISABLE");
                key.SetValue("iexplore.exe", 0);
                key.Close();
                root.Close();
                return true;
            }
            catch (Exception ex)
            {
                MiscLib.Debug(ex.Message);
                return false;
            }
        }
        
        public static void WaitS(int iTimeOutS)
        {
            DateTime dtOld = System.DateTime.Now;
            DateTime dtCur = dtOld;
            while (true)
            {
                Application.DoEvents();
                dtCur = System.DateTime.Now;
                TimeSpan diff = dtCur.Subtract(dtOld);
                if ((diff.TotalSeconds >= iTimeOutS))
                {
                    break;
                }
            }
        }
        /*
        public static bool CheckInternetConnection()
        {
            string[] sTestingURLList = new string[] { "http://www.google.com", "http://www.msn.com", "http://www.baidu.com" };
            HttpWebRequest oRequest = null;
            bool bHasInternetConnection = false;
            foreach (string sURL in sTestingURLList)
            {
                Utility.MiscLib.WaitS(5);
                Debug("Try to access " + sURL);
                try
                {
                    oRequest = (HttpWebRequest)WebRequest.Create(sURL);
                }
                catch (Exception exp)
                {
                    Debug(exp.Message);
                    return false;
                }

                ASCIIEncoding encoding = new ASCIIEncoding();
                oRequest.Method = "GET";
                oRequest.KeepAlive = false;
         */
                //oRequest.Accept = @"image/gif, image/x-xbitmap, image/jpeg, image/pjpeg, application/x-shockwave-flash, application/xaml+xml, application/vnd.ms-xpsdocument, application/x-ms-xbap, application/x-ms-application, application/x-silverlight, */*";
                /*
                oRequest.Headers.Add("Accept-Encoding: gzip, deflate");
                oRequest.UserAgent = @"Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)";
                WebResponse oResponse = null;
                try
                {
                    oResponse = oRequest.GetResponse();
                }
                catch (WebException webExp)
                {
                    Debug(webExp.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Debug(e.Message);
                    continue;
                }
                bHasInternetConnection = true;
                break;
            }
            return bHasInternetConnection;
        }
        */
    }
}
