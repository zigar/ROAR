
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using Utility;

namespace HNAP
{
    public enum AS_TYPE { ALLOW_ALL, BLOCK_ALL, BLOCK_TODAY, BLOCK_CUR_HALF, BLOCK_NEXT_HALF, BLOCK_NEXT_HOUR };
    public enum DUT_TYPE { CES, X, VIPER };

    public struct TaskExtensions
    {
        public string Name;
        public string URL;
        public string Type;
    }

    public struct MACInfo
    {
        public string MacAddress;
        public string DeviceName;
    }

    public class DNSEntry
    {
        public string Primary = "0.0.0.0";
        public string Secondary = "0.0.0.0";
        public string Tertiary = "0.0.0.0";
    }

    public class WANSettings
    {
        public string Type;
        public string Username;
        public string Password;
        public int MaxIdleTime;
        public string ServiceName;
        public bool AutoReconnect;
        public string IPAddress;
        public string SubnetMask;
        public string Gateway;
        public DNSEntry DNS;
        public string MacAddress;
        public int MTU;
    }

    public class DeviceInfo
    {
        public string Type;
        public string DeviceName;
        public string VendorName;
        public string ModelDecription;
        public string ModelName;
        public string FirmwareVersion;
        public string PresentationURL;
        public string[] SOAPActions;
        public string[] SubDeviceURLs;
        public TaskExtensions[] Tasks;
        public string SerialNumber;
        public string TimeZone;
        public bool AutoAdjustDST;
        public string Locale;
        public string UserName;
        public bool SSL;
        public string[] SupportLocales;
        public DeviceInfo Clone()
        {
            return (DeviceInfo)(this.MemberwiseClone());
        }
    }

    public class SecurityInfo
    {
        public string SecurityType;
        public string[] Encryptions;
        public SecurityInfo Clone()
        {
            return (SecurityInfo)(this.MemberwiseClone());
        }
    }

    public class WideChannel
    {
        public int Channel;
        public int[] SecondaryChannels;
        public WideChannel Clone()
        {
            return (WideChannel)(this.MemberwiseClone());
        }
    }

    public class RadioInfo
    {
        public string RadioID;
        public int Frequency;
        public string[] SupportedModes;
        public int[] Channels;
        public WideChannel[] WideChannels;
        public SecurityInfo[] SupportedSecurity;
        public RadioInfo Clone()
        {
            return (RadioInfo)(this.MemberwiseClone());
        }
    }

    class RadioIDSetting
    {
        public string RadioID = null;
        public bool Enabled = false;
        public string Mode = null;
        public string MacAddress = null;
        public string SSID = null;
        public bool SSIDBroadCast = false;
        public int ChannelWidth = 0;
        public int Channel = 0;
        public int SecondaryChannel = 0;
        public bool QoS = false;

        public RadioIDSetting Clone()
        {
            return (RadioIDSetting)(this.MemberwiseClone());
        }
    };

    class RadioIDSecuritySetting
    {
        public string RadioID = "";
        public bool Enabled = false;
        public string Type = "";
        public string Encryption = "";
        public string Key = "";
        public int KeyRenewal = 3600;
        public string RadiusSecret1 = "";
        public string RadiusIP1 = "0.0.0.0";
        public int RadiusPort1 = 0;
        public string RadiusSecret2 = "";
        public string RadiusIP2 = "0.0.0.0";
        public int RadiusPort2 = 0;

        public RadioIDSecuritySetting Clone()
        {
            return (RadioIDSecuritySetting)(this.MemberwiseClone());
        }
    };

    class GASetting
    {
        public bool Enabled = false;
        public string SSID = "";
        public string Password = "";
        public int MaxGuestsAllowed = 0;
        public bool CanBeActive = false;
    }

    class WirelessSetting
    {
        public string SSID = "";
        public string Key = "";
        public string GuestSSID = "";
        public bool GuestEnabled = false;
        public string GuestPassword = "";
        public int MaxGuestsAllowed = 0;
    }

    public class LocalAuthenticationType
    {
        public string Authentication;
        public string keyType;
        public LocalAuthenticationType()
        {
        }
        public LocalAuthenticationType(string auth, string key)
        {
            Authentication = auth;
            keyType = key;
        }
        public LocalAuthenticationType Clone()
        {
            return (LocalAuthenticationType)(this.MemberwiseClone());
        }
    }

    public class AuthenticationMap
    {
        public string RemoteSecurityType;
        public LocalAuthenticationType[] LocalSecurityType;

    }

    public class EncryptionMap
    {
        public string RemoteEncryption;//WEP-64 ....
        public string[] LocalEncryption;
        public EncryptionMap(string remote, string[] local)
        {
            RemoteEncryption = remote;
            LocalEncryption = local;
        }
        public EncryptionMap()
        {
        }
    }

    public class ProfileParam
    {
        public string Authentication;
        public string Encryption;
        public string KeyType;
        public string KeyMaterial;

        public ProfileParam(string auth, string encrpt, string type, string key)
        {
            Authentication = auth;
            Encryption = encrpt;
            KeyType = type;
            KeyMaterial = key;
        }
    }

    public struct Device
    {
        public string MacAddress;
        public string IPAddress;
        public string IPStart;
        public string IPEnd;
    };

    public struct Weekdays
    {
        public string Sunday;
        public string Monday;
        public string Tuesday;
        public string Wednesday;
        public string Thursday;
        public string Friday;
        public string Saturday;
    };

    public struct AccessPolicyResult
    {
        public string AccessPolicyNumber;
        public string Result;
    };

    public struct AccessSchedule
    {
        public string Sunday;
        public string Monday;
        public string Tuesday;
        public string Wednesday;
        public string Thursday;
        public string Friday;
        public string Saturday;
        private DateTime CurrentTime;
        /*
        public AccessSchedule(AS_TYPE type)
        {
            Sunday = null;
            Monday = null;
            Tuesday = null;
            Wednesday = null;
            Thursday = null;
            Friday = null;
            Saturday = null;
            CurrentTime = MiscLib.GetNTPUTCTime();

            if (CurrentTime == DateTime.MinValue)
            {
                if (type != AS_TYPE.ALLOW_ALL && type != AS_TYPE.BLOCK_ALL)
                {
                    Utility.MiscLib.Debug("Set the Access Schedule to null");
                    return;
                }
            }

            HNAP_1_2_Wrap hnap_1_2_client = new HNAP_1_2_Wrap();

            switch (type)
            {
                case AS_TYPE.ALLOW_ALL:
                    AllowAll();
                    break;
                case AS_TYPE.BLOCK_ALL:
                    BlockAll();
                    break;
                case AS_TYPE.BLOCK_TODAY:
                    AllowAll();

                    if (!AdjustTimeZone())
                    {
                        return;
                    }

                    SkipSensitiveTimeSlot(type);

                    string timeSlot = null;
                    for (int i = 0; i < 48; i++)
                    {
                        timeSlot += "0";
                    }

                    SetTimeSlot(CurrentTime, timeSlot);
                    break;
                case AS_TYPE.BLOCK_CUR_HALF:
                    AllowAll();

                    if (!AdjustTimeZone())
                    {
                        return;
                    }

                    SkipSensitiveTimeSlot(type);

                    int hour = CurrentTime.Hour;
                    int minute = CurrentTime.Minute;

                    timeSlot = null;

                    for (int i = 0; i < 24; i++)
                    {
                        if (i != hour)
                        {
                            timeSlot += "11";
                        }
                        else
                        {
                            if (minute < 30)
                            {
                                timeSlot += "01";
                            }
                            else
                            {
                                timeSlot += "10";
                            }
                        }

                    }
                    SetTimeSlot(CurrentTime, timeSlot);
                    break;
                case AS_TYPE.BLOCK_NEXT_HALF:
                    AllowAll();

                    if (!AdjustTimeZone())
                    {
                        return;
                    }

                    SkipSensitiveTimeSlot(type);

                    DateTime NextTime = CurrentTime.AddMinutes(30);

                    hour = NextTime.Hour;
                    minute = NextTime.Minute;

                    timeSlot = null;

                    for (int i = 0; i < 24; i++)
                    {
                        if (i != hour)
                        {
                            timeSlot += "11";
                        }
                        else
                        {
                            if (minute < 30)
                            {
                                timeSlot += "01";
                            }
                            else
                            {
                                timeSlot += "10";
                            }
                        }

                    }

                    SetTimeSlot(NextTime, timeSlot);
                    break;
                case AS_TYPE.BLOCK_NEXT_HOUR:
                    AllowAll();

                    if (!AdjustTimeZone())
                    {
                        return;
                    }

                    SkipSensitiveTimeSlot(type);

                    DateTime NextHour = CurrentTime.AddHours(1);

                    hour = NextHour.Hour;

                    timeSlot = null;

                    for (int i = 0; i < 24; i++)
                    {
                        if (i != hour)
                        {
                            timeSlot += "11";
                        }
                        else
                        {
                            timeSlot += "00";
                        }

                    }

                    SetTimeSlot(NextHour, timeSlot);
                    break;
            }
        }
        */
        public static bool IsNull(AccessSchedule AS)
        {
            if (AS.Monday == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SetTimeSlot(DateTime DT, string timeSlot)
        {
            switch (DT.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    Monday = timeSlot;
                    break;
                case DayOfWeek.Tuesday:
                    Tuesday = timeSlot;
                    break;
                case DayOfWeek.Wednesday:
                    Wednesday = timeSlot;
                    break;
                case DayOfWeek.Thursday:
                    Thursday = timeSlot;
                    break;
                case DayOfWeek.Friday:
                    Friday = timeSlot;
                    break;
                case DayOfWeek.Saturday:
                    Saturday = timeSlot;
                    break;
                default:
                    Sunday = timeSlot;
                    break;
            }
        }
        /*
        private bool AdjustTimeZone()
        {
            HNAP_1_2_Wrap hnap_1_2_client = new HNAP_1_2_Wrap();
            DeviceInfo devInfo = hnap_1_2_client.GetDeviceInfo();

            if (devInfo.TimeZone != "UTC" || devInfo.AutoAdjustDST)
            {
                string ret = hnap_1_2_client.SetDeviceSettings2("UTC", devInfo.Locale, devInfo.UserName, devInfo.SSL, false);
                if (ret == "OK" || ret == "REBOOT")
                {
                    Console.WriteLine("The time zone is changed to UTC with AutoAdjustDST false");
                    hnap_1_2_client.WaitingRebootSync();
                    return true;
                }
                else
                {
                    Console.WriteLine("The time zone is not changed to UTC with AutoAdjustDST false.");
                    Console.WriteLine("This may affect the test result.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("The time zone is already UTC with AutoAdjustDST false.");
                return true;
            }
        }
        */
        private void AllowAll()
        {
            string timeSlot = null;
            for (int i = 0; i < 48; i++)
            {
                timeSlot += "1";
            }
            Monday = timeSlot;
            Sunday = timeSlot;
            Tuesday = timeSlot;
            Wednesday = timeSlot;
            Thursday = timeSlot;
            Friday = timeSlot;
            Saturday = timeSlot;
        }

        private void BlockAll()
        {
            string timeSlot = null;
            for (int i = 0; i < 48; i++)
            {
                timeSlot += "0";
            }
            Monday = timeSlot;
            Sunday = timeSlot;
            Tuesday = timeSlot;
            Wednesday = timeSlot;
            Thursday = timeSlot;
            Friday = timeSlot;
            Saturday = timeSlot;
        }
        /*
        private void SkipSensitiveTimeSlot(AS_TYPE type)
        {
            CurrentTime = MiscLib.GetNTPUTCTime();

            if (CurrentTime == DateTime.MinValue)
            {
                Console.WriteLine("The current time is not got successfully from NTP server.");
                return;
            }

            if (type == AS_TYPE.BLOCK_CUR_HALF || type == AS_TYPE.BLOCK_NEXT_HALF || type == AS_TYPE.BLOCK_NEXT_HOUR)
            {
                if ((CurrentTime.TimeOfDay.Minutes >= 25 && CurrentTime.TimeOfDay.Minutes < 30) || CurrentTime.TimeOfDay.Minutes >= 55)
                {
                    Console.WriteLine("The time is on the edge of a new hour section.");
                    Console.WriteLine("Wait for 10 minutes to avoid timing issue.");
                    Thread.Sleep(10000 * 60);
                }
            }
            else if (type == AS_TYPE.BLOCK_TODAY)
            {
                if (CurrentTime.TimeOfDay.Hours == 23 && CurrentTime.TimeOfDay.Minutes >= 55)
                {
                    Console.WriteLine("The time is on the edge of a new day.");
                    Console.WriteLine("Wait for 10 minutes to avoid timing issue.");
                    Thread.Sleep(10000 * 60);
                }
            }

            //Set current time again after skipping the sensitive time slot
            CurrentTime = MiscLib.GetNTPUTCTime();

            if (CurrentTime == DateTime.MinValue)
            {
                Console.WriteLine("The current time is not got successfully from NTP server.");
                return;
            }
        }
        */
        public bool CompareWith(AccessSchedule Other)
        {
            if (this.Sunday != Other.Sunday)
            {
                return false;
            }
            if (this.Monday != Other.Monday)
            {
                return false;
            }
            if (this.Tuesday != Other.Tuesday)
            {
                return false;
            }
            if (this.Wednesday != Other.Wednesday)
            {
                return false;
            }
            if (this.Thursday != Other.Thursday)
            {
                return false;
            }
            if (this.Friday != Other.Friday)
            {
                return false;
            }
            if (this.Saturday != Other.Saturday)
            {
                return false;
            }
            return true;
        }
        /*
        public override string ToString()
        {
            Dictionary<int, string> timeSlotDic = new Dictionary<int, string>();

            if (this.CompareWith(new AccessSchedule(AS_TYPE.ALLOW_ALL)))
            {
                return "Allow all";
            }

            if (this.CompareWith(new AccessSchedule(AS_TYPE.BLOCK_ALL)))
            {
                return "Block all";
            }

            timeSlotDic.Add(0, "00:00");
            timeSlotDic.Add(1, "00:30");
            timeSlotDic.Add(2, "01:00");
            timeSlotDic.Add(3, "01:30");
            timeSlotDic.Add(4, "02:00");
            timeSlotDic.Add(5, "02:30");
            timeSlotDic.Add(6, "03:00");
            timeSlotDic.Add(7, "03:30");
            timeSlotDic.Add(8, "04:00");
            timeSlotDic.Add(9, "04:30");
            timeSlotDic.Add(10, "05:00");
            timeSlotDic.Add(11, "05:30");
            timeSlotDic.Add(12, "06:00");
            timeSlotDic.Add(13, "06:30");
            timeSlotDic.Add(14, "07:00");
            timeSlotDic.Add(15, "07:30");
            timeSlotDic.Add(16, "08:00");
            timeSlotDic.Add(17, "08:30");
            timeSlotDic.Add(18, "09:00");
            timeSlotDic.Add(19, "09:30");
            timeSlotDic.Add(20, "10:00");
            timeSlotDic.Add(21, "10:30");
            timeSlotDic.Add(22, "11:00");
            timeSlotDic.Add(23, "11:30");
            timeSlotDic.Add(24, "12:00");
            timeSlotDic.Add(25, "12:30");
            timeSlotDic.Add(26, "13:00");
            timeSlotDic.Add(27, "13:30");
            timeSlotDic.Add(28, "14:00");
            timeSlotDic.Add(29, "14:30");
            timeSlotDic.Add(30, "15:00");
            timeSlotDic.Add(31, "15:30");
            timeSlotDic.Add(32, "16:30");
            timeSlotDic.Add(33, "16:30");
            timeSlotDic.Add(34, "17:00");
            timeSlotDic.Add(35, "17:30");
            timeSlotDic.Add(36, "18:00");
            timeSlotDic.Add(37, "18:30");
            timeSlotDic.Add(38, "19:00");
            timeSlotDic.Add(39, "19:30");
            timeSlotDic.Add(40, "20:00");
            timeSlotDic.Add(41, "20:30");
            timeSlotDic.Add(42, "21:00");
            timeSlotDic.Add(43, "21:30");
            timeSlotDic.Add(44, "22:00");
            timeSlotDic.Add(45, "22:30");
            timeSlotDic.Add(46, "23:00");
            timeSlotDic.Add(47, "23:30");
            timeSlotDic.Add(48, "24:00");



            string[] weekDays = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string rtnString = null;
            foreach (string day in weekDays)
            {
                bool start = false;
                string blockTimeSlot = null;
                int begin = -1;

                string dayValue = (string)GetType().GetField(day).GetValue(this);
                for (int i = 0; i < dayValue.Length; i++)
                {
                    if (dayValue[i] == '0' && i < dayValue.Length - 1 && !start)
                    {
                        begin = i;
                        start = true;
                    }
                    else if (dayValue[i] == '1' && start)
                    {
                        string beginTimeSlot = null;
                        string endTimeSlot = null;

                        timeSlotDic.TryGetValue(begin, out beginTimeSlot);
                        timeSlotDic.TryGetValue(i, out endTimeSlot);
                        if (blockTimeSlot != null)
                        {
                            blockTimeSlot = string.Join(",", new string[] { blockTimeSlot, beginTimeSlot + "-" + endTimeSlot });
                        }
                        else
                        {
                            blockTimeSlot += beginTimeSlot + "-" + endTimeSlot;
                        }

                        start = false;
                    }
                    else if (dayValue[i] == '0' && i == dayValue.Length - 1 && start)
                    {
                        string beginTimeSlot = null;
                        string endTimeSlot = null;

                        timeSlotDic.TryGetValue(begin, out beginTimeSlot);
                        timeSlotDic.TryGetValue(i + 1, out endTimeSlot);
                        if (blockTimeSlot != null)
                        {
                            blockTimeSlot = string.Join(",", new string[] { blockTimeSlot, beginTimeSlot + "-" + endTimeSlot });
                        }
                        else
                        {
                            blockTimeSlot += beginTimeSlot + "-" + endTimeSlot;
                        }

                        start = false;
                    }
                    else if (dayValue[i] == '0' && i == dayValue.Length - 1 && !start)
                    {
                        string beginTimeSlot = null;
                        string endTimeSlot = null;

                        timeSlotDic.TryGetValue(i, out beginTimeSlot);
                        timeSlotDic.TryGetValue(i + 1, out endTimeSlot);
                        if (blockTimeSlot != null)
                        {
                            blockTimeSlot = string.Join(",", new string[] { blockTimeSlot, beginTimeSlot + "-" + endTimeSlot });
                        }
                        else
                        {
                            blockTimeSlot += beginTimeSlot + "-" + endTimeSlot;
                        }

                        start = false;
                    }
                }

                if (!string.IsNullOrEmpty(blockTimeSlot))
                {
                    if (rtnString != null)
                    {
                        rtnString = string.Join(",", new string[] { rtnString, day + ":" + blockTimeSlot });
                    }
                    else
                    {
                        rtnString += day + ":" + blockTimeSlot;
                    }

                }
            }
            return "Block time slot: " + rtnString + "(Current UTC Time: " + CurrentTime + "(" + CurrentTime.DayOfWeek + "))";
        }
        */
    };

    public struct AccessPolicy
    {
        public int AccessPolicyNumber;
        public string PolicyName;
        public string Status;
        public Device[] AppliedDeviceList;
        public AccessSchedule AccessSchedule;
        public string[] BlockedURL;
        public string[] BlockedKeyword;
        public int[] BlockedCategory;
    };

    public struct AccessPolicy2
    {
        public int AccessPolicyNumber;
        public string PolicyName;
        public string Status;
        public Device[] AppliedDeviceList;
        public AccessSchedule AccessSchedule;
        public string[] BlockedURL;
        public string[] AllowedURL;
        public string[] BlockedKeyword;
        public int[] BlockedCategory;
    }

    public class PolicyCapabilities
    {
        public int FirstAccessPolicyNumber = 0;
        public int MaxPolicyNumber = 0;
        public int MaxPolicyName = 0;
        public int MaxAppliedDeviceList = 0;
        public int MaxBlockedURLArray = 0;
        public int MaxBlockedURLString = 0;
        public int MaxAllowedURLArray = 0;
        public int MaxAllowedURLString = 0;
        public int MaxBlockedKeywordArray = 0;
        public int MaxBlockedKeywordString = 0;
        public int MaxBlockedCategoryArray = 0;
    }

    public struct WTP_URL
    {
        public string URL;
        public int Score;
    }

    public class RadiusInfo
    {
        public string ServerIP;
        public int ServerPort;
        public string Key;
        public string UserName;
        public string Password;

        public RadiusInfo()
        {
            ServerIP = "10.74.52.14";
            ServerPort = 1812;
            Key = "cisco";
            UserName = "radiustest";
            Password = "12345678";
        }
    }

    public class WLNSettings
    {
        public string SSID;
        public bool SecurityEnabled = false;
        public string Authentication;
        public string Type;
        public string Encryption;
        public string Key;
        public string Username;
        public string Password;
        public bool RadiusEnabled = false;
    }
}

namespace Utility
{
    public class IPv4Info
    {
        public string IPAddr;
        public string NetMask;
        public string Gateway;
        public string DHCPServer;
        public string DNSServer;

        public IPv4Info()
        {
            this.IPAddr = "0.0.0.0";
            this.NetMask = "0.0.0.0";
            this.Gateway = "0.0.0.0";
            this.DNSServer = "0.0.0.0";
        }

        public IPv4Info(string IPAddr, string NetMask)
        {
            this.IPAddr = IPAddr;
            this.NetMask = NetMask;
        }

        public IPv4Info(string IPAddr, string NetMask, string Gateway)
        {
            this.IPAddr = IPAddr;
            this.NetMask = NetMask;
            this.Gateway = Gateway;
        }

        public IPv4Info(string IPAddr, string NetMask, string Gateway, string DNSServer)
        {
            this.IPAddr = IPAddr;
            this.NetMask = NetMask;
            this.Gateway = Gateway;
            this.DNSServer = DNSServer;
        }
    }

    public class WirelessSecurityPSK
    {
        public string Authentication;
        public string Type;
        public string Encryption;
        public string Key;
    }

    public class WirelessSecurityEnterprise
    {
        public string Authentication;
        public string Encryption;
        public string Username;
        public string Password;
    }

    public class ModuleStatistics
    {
        public string modulename = null;
        public int pass = 0;
        public int fail = 0;
        public int skip = 0;

        public ModuleStatistics(string name)
        {
            modulename = name;
        }

    }

    public class Platform
    {
        public string PlatformID;
        public string Title;

        public Platform()
        {

        }

        public Platform(string PlatformID, string Title)
        {
            this.PlatformID = PlatformID;
            this.Title = Title;
        }
    }

    public class Config
    {
        public string ConfigID;
        public string Title;

        public Config()
        {

        }

        public Config(string ConfigID, string Title)
        {
            this.ConfigID = ConfigID;
            this.Title = Title;
        }
    }

}
