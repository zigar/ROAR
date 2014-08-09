ROAR
====
I love Programming, and Piano. so P&P?

Enjoy the feeling of getting stronger everyday!

This repository contains following projects:


WIFIAPI
-----

This project is to provide a command line interface for user to get status/control his/her WIFI card on different platform(Aka Windows and Android phone in my implementation)

Windows API is developed with C# in VS, Android API is developed with ADT.

Take WIFIAPI(Windows) for example, when you run it in your host, it will start a TCP socket in port 2014, and then you can connect to this port like below:


ZHIQIN-M-Q0FF:~ zygar$ telnet x.x.x.x 2014


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



For Android phone, you will need to connect it to a host with USB debug mode. and then ADB forward is required to map android's port to host's link local port, at last a map tool to external ip address is required as well.


