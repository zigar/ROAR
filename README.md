WIFI_CLI
====

Brief Introduction
-----

This project aime to provide a command line tool for controlling WIFI network cards across different platform(Windows and Android phone). For windows it use [Micosoft Windows Native Wifi API](https://docs.microsoft.com/en-us/windows/desktop/NativeWiFi/portal) )  

Windows API is developed with C# in VS, Android API is developed with ADT.

Example usage
-----

Take WIFIAPI(Windows) for example, when you run it in your host, it will start a TCP socket in port 2014, and then you can connect to this port like below:

```
~ zigar$ telnet x.x.x.x 2014


Trying x.x.x.x


Connected to x.x.x.x.


Escape character is '^]'.


RangeMax Dual Band Wireless-N USB Adapter#: please input command, type help to see available commands:


RangeMax Dual Band Wireless-N USB Adapter#: help


help       - display supported command


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
```


For Android phone, you will need to connect it to a host with USB debug mode. and then ADB forward is required to map android's port to host's link local port, at last a map tool to external ip address is required as well, I use passport.


