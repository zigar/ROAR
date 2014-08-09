package com.example.wifitestpro;

import java.util.BitSet;
import java.util.Iterator;
import java.util.List;

import android.content.Intent;
import android.net.wifi.ScanResult;
import android.net.wifi.WifiConfiguration;
import android.net.wifi.WifiInfo;
import android.net.wifi.WifiManager;
import android.os.Bundle;
import android.support.v4.app.Fragment;
import android.support.v7.app.ActionBarActivity;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import android.os.Handler;

public class DisplayMessageActivity extends ActionBarActivity {

	private WifiInfo mWifiInfo;
	private List<WifiConfiguration> mWifiConfiguration;
    private List<ScanResult> mWifiList;
    
	public WifiManager mWifiManager;
	
	private TextView myTextView;
	
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
			
		//Get the text message from the intent
		Intent intent = getIntent();
		
		WifiAdmin wifiAdmin = new WifiAdmin(this);  
        wifiAdmin.openWifi();  
        boolean success = wifiAdmin.addNetwork(wifiAdmin.CreateWifiInfo("qqqq", "11111111", 3));
        
            	WifiManager wifiManager = (WifiManager) getSystemService(WIFI_SERVICE); 
            	
            	
            	
        		mWifiManager = wifiManager;
        		
        		
        		
        		if(!this.mWifiManager.isWifiEnabled()){ 
        			String warning = "wifi is disabled, no wifi info\n";
        			setContentView(R.layout.fragment_display_message);
        			TextView myTextView = (TextView)findViewById(R.id.myTextView);
        			myTextView.setText(warning);
        			return;
                    //this.mWifiManager.setWifiEnabled(true);
                }
        		
        		WifiConfiguration localwificfg = isExsits("qqqq");
            	while (localwificfg == null) localwificfg = isExsits("qqqq");
        		
            	WifiInfo info = wifiManager.getConnectionInfo();
            	Log.d("!!!wifiInfo",info.toString());
        		int ipAddress = info.getIpAddress();
        		String IpAddr = String.format("%d.%d.%d.%d",(ipAddress & 0xff),(ipAddress >> 8 & 0xff),(ipAddress >> 16 & 0xff),(ipAddress >> 24 & 0xff));
        		System.out.println("!!!!!! IP="+IpAddr);

        		String MacAddr = info.getMacAddress();
        		System.out.println("!!!!!! Mac="+MacAddr);

        		String bssid = info.getBSSID();
        		System.out.println(bssid);
        		
        		int networkId = info.getNetworkId();
        		System.out.println("!!!!!! networkId="+networkId);
        		
        		String ssid = info.getSSID();
        		System.out.println("!!!!!! ssid="+ssid);
        		
        		int linkspeed = info.getLinkSpeed();
        		System.out.println("!!!!!!linkspeed="+linkspeed);
        		
        		int rssi = info.getRssi();
        		System.out.println("!!!!!!rssi="+rssi);
        		
        		String wifiStatus = "wifi status unknown";
        		
        		
        		String wificfg = localwificfg.toString();
        		System.out.println("!!!!wifi cfg"+wificfg);
        		
        			
        		String message = intent.getStringExtra(MainActivity.EXTRA_MESSAGE);
        		if(!wifiManager.isWifiEnabled()){ 
        	             wifiStatus = "disabled";
        	             System.out.println("wifi status is:" + wifiStatus);
        		} else {
        	    	     wifiStatus = "enabled";
        	    	     System.out.println("wifi status is:" + wifiStatus);
        	    }
        		
        		setWifiList();
        		StringBuilder scanList = lookUpScan();
        		System.out.println("scan list : " + scanList);
        		
        		message +=  "\n\n" + 
        		            "######### Local WiFi Info ##########" + "\n\n" +
        	                "Wifi Status: " + wifiStatus + "\n" +
        				    "SSID: "  + ssid + "\n" +
        				    "MAC Address: " + MacAddr + "\n" +
        				    "IP Address" + IpAddr + "\n" +
        				    "Link Speed: " + linkspeed + "\n" +
        				    "RSSI:" + rssi + "\n" +
        				    "BSSID:" + bssid + "\n\n\n" +
        				    "WIFI Configuration" + wificfg + "\n\n\n" +
        				    "########## WIFI Scan List ##########" + "\n\n" +
        				    scanList;
        		
        		//Create the text view
        		//TextView textView = new TextView(this);
        		//textView.setTextSize(25);
        		//textView.setText(message);

        		// Set the text view as the activity layout
        		//setContentView(textView);
        		
        		setContentView(R.layout.fragment_display_message);
        		TextView myTextView = (TextView)findViewById(R.id.myTextView);
        		
        		//myTextView.setText(String.valueOf(success));
        		myTextView.setText(message);
        
	}
	
	
	/*
	 private WifiConfiguration isExsits(String str) {
	        Iterator localIterator = this.mWifiManager.getConfiguredNetworks().iterator();
	        WifiConfiguration localWifiConfiguration;
	        do {
	            if(!localIterator.hasNext()) return null;
	            localWifiConfiguration = (WifiConfiguration) localIterator.next();
	        }while(!localWifiConfiguration.SSID.equals("\"" + str + "\""));
	        return localWifiConfiguration;
	  }
*/
	private WifiConfiguration isExsits(String SSID)    
    {    
        List<WifiConfiguration> existingConfigs = mWifiManager.getConfiguredNetworks();    
           for (WifiConfiguration existingConfig : existingConfigs)     
           {    
             if (existingConfig.SSID.equals("\"" + SSID + "\"")) 
        	 //if (existingConfig.SSID.equals(SSID))
             {    
                 return existingConfig;    
             }    
           }    
        return null;     
    }  
	
	public StringBuilder lookUpScan() {
	        StringBuilder localStringBuilder = new StringBuilder();
	        for (int i = 0; i < mWifiList.size(); i++)
	        {
	            localStringBuilder.append("Index_"+new Integer(i + 1).toString() + ":");
	            localStringBuilder.append((mWifiList.get(i)).toString());
	            localStringBuilder.append("\n\n");
	        }
	        return localStringBuilder;
	}

	 public void setWifiList() {
	        this.mWifiList = this.mWifiManager.getScanResults();
	 }
	

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {

		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.display_message, menu);
		return true;
		// MenuInflater inflater = getMenuInflater();
		//    inflater.inflate(R.menu.main_activity_actions, menu);
		//    return super.onCreateOptionsMenu(menu);
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		// Handle action bar item clicks here. The action bar will
		// automatically handle clicks on the Home/Up button, so long
		// as you specify a parent activity in AndroidManifest.xml.
		int id = item.getItemId();
		if (id == R.id.action_settings) {
			return true;
		}
		return super.onOptionsItemSelected(item);
	}

	/**
	 * A placeholder fragment containing a simple view.
	 */
	public static class PlaceholderFragment extends Fragment {

		public PlaceholderFragment() {
		}

		@Override
		public View onCreateView(LayoutInflater inflater, ViewGroup container,
				Bundle savedInstanceState) {
			View rootView = inflater.inflate(R.layout.fragment_display_message,
					container, false);
			return rootView;
		}
	}

}
