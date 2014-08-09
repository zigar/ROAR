package com.example.wifitestpro;

import java.net.*;
import java.io.*;

import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.impl.client.DefaultHttpClient;
 
public class SocketProtocol {
   
 
 
    public String processInput(WifiAdmin wifiAdmin , String theInput) {
    	String theOutput = "";
    	String prompt = android.os.Build.MANUFACTURER + "_"+android.os.Build.PRODUCT + "#: ";
    	String[] cmd = null;
    	if (theInput == null || theInput =="") 
    		{
    			theOutput = String.format(prompt + "please input command, type help to see available commands:%n" + prompt, "");
    			return theOutput;
    		}
    	cmd = theInput.split(" ");
    	
    	if (cmd[0].equalsIgnoreCase("exit")) theOutput = "EOF";
    	else if (cmd[0].equalsIgnoreCase("help")) theOutput = String.format( "help		- display supported command%n"
    			+ "openwifi		- enable wifi card%n"
    			+ "closewifi	- close wifi card%n"
    			+ "listnt		- list available wireless network%n"
    			+ "associate	- associate with specified wireless network%n"
    			+ "disasso		- disassociate wireless on sepcified wifi card%n"
    			+ "getip		- get ip address info%n"
    			//+ "getrate		- get wifi interface Tx/Rx rate%n"
    			+ "httpget		- http get method for a specified URL%n"
    			+ "exit		- exit login%n" + prompt, "");
    	try{
    	if (cmd[0].equalsIgnoreCase("openwifi"))
    	{
    		wifiAdmin.openWifi();
    		theOutput = String.format("Success%n+ prompt");
    	}
    	else if (cmd[0].equalsIgnoreCase("closewifi"))
    	{
    		wifiAdmin.closeWifi();
    		theOutput = String.format("Success%n+ prompt");
    	}
    	else if (cmd[0].equalsIgnoreCase("associate"))
    	{
    		if (cmd.length != 4) { theOutput =String.format( "associate  <ssid> <password> <type(1:no security, 2:WEP, 3:WPA)>%n"+ prompt); return theOutput; }
            
    		boolean result = wifiAdmin.addNetwork(wifiAdmin.CreateWifiInfo(cmd[1], cmd[2],Integer.parseInt(cmd[3])));
    		theOutput = result? String.format("Success%n"+ prompt): String.format("Fail%n"+ prompt);
    	}
    	else if (cmd[0].equalsIgnoreCase("disasso"))
    	{
    		wifiAdmin.disconnectWifi(0);
    		theOutput = String.format("Success%n"+ prompt);
    	}
    	else if (cmd[0].equalsIgnoreCase("listnt"))
    	{
    		wifiAdmin.startScan();
    		
    		StringBuilder sb = wifiAdmin.lookUpScan();
    		theOutput = sb.toString()+"\n"+ prompt;
    	}
    	else if (cmd[0].equalsIgnoreCase("getip")) {  
    	    return wifiAdmin.getIPAddress()  +"\n"+ prompt;
    	}  
    	
    	//else if (cmd[0].equalsIgnoreCase("getrate")) {  
    	//    return wifiAdmin.getWifiRate()+"\n";  
    	//} 
    	else if (cmd[0].equalsIgnoreCase("httpget")) { 
    		String uri = "";
    		if (!cmd[1].startsWith("http://"))
    			uri = "http://" + cmd[1];
    	    return httpget(uri) +"\n"+ prompt;  
    	} 
    	}
    	catch (Exception e)
    	{
    		return e.toString() +"\n"+ prompt;
    	}
    	
        return theOutput;
    }
    
    public String httpget(String urlToRead) {
        URL url;
        HttpURLConnection conn;
        BufferedReader rd;
        String line;
        String result = "";
        try {
           url = new URL(urlToRead);
           conn = (HttpURLConnection) url.openConnection();
           conn.setRequestMethod("GET");
           rd = new BufferedReader(new InputStreamReader(conn.getInputStream()));
           while ((line = rd.readLine()) != null) {
              result += line;
           }
           rd.close();
           conn.disconnect();
        
        } catch (Exception e) {
        	return e.toString() +"\n";
        }
        return result;
     }
}

