package com.example.wifitestpro;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.PrintWriter;
import java.io.Writer;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketTimeoutException;
import java.util.Scanner;

import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.support.v4.app.Fragment;
import android.support.v7.app.ActionBarActivity;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import android.widget.Toast;


public class TestServerActivity extends ActionBarActivity {

	private TextView myTextView;
	public static final int TIMEOUT=100;
	private String connectionStatus=null;
	private Handler mHandler=null;
	public ServerSocket server=null;
	public Scanner socketIn=null;
	public PrintWriter socketOut=null;
	public static final String TAG="Connection";
	public boolean connected=false;
	public static final int port=38300;
	public volatile boolean exit = false; 
	public Thread socketThread;
	public WifiAdmin wifiAdmin ;
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_test_server);

		wifiAdmin = new WifiAdmin(this);
		Intent intent = getIntent();
		String message = intent.getStringExtra(MainActivity.EXTRA_MESSAGE);
	
		Toast.makeText(TestServerActivity.this, message, message.length()).show();
		
		Log.d("WifiTestServer", "!!!TestServerActivity "+message);
		
		
			
		if (message.equalsIgnoreCase("start")) {
			String messg = "start server command received\n";
		    Log.d("WifiTestServer", messg);
		    setContentView(R.layout.fragment_test_server);
			
			myTextView = (TextView)findViewById(R.id.myTextView3);
			
			myTextView.append(messg);
			
		    String msg="Waiting for connnection from client...";
		    Toast.makeText(TestServerActivity.this, msg, msg.length()).show();
		   
		   
		    mHandler = new Handler();
		    socketThread = new Thread(startserver);
		    socketThread.start();
		    
		    
	
		} else if (message.equalsIgnoreCase("stop")) {
			
			String messg = "stop server command received\n";
			Log.d("WifiTestServer", messg);
			setContentView(R.layout.fragment_test_server);
				
		    myTextView = (TextView)findViewById(R.id.myTextView3);
		
			myTextView.append(messg);
			
				try {

		
			socketThread.interrupt();
				} catch (Exception e) {
					 e.printStackTrace();
					
				}
			
			

		} else {
			Log.d("WifiTestServer", "unknown server op command!");
			String msg="unknown server op command!";
		    Toast.makeText(TestServerActivity.this, msg, msg.length()).show();
		    return;
			
			
		}
			
		//setContentView(R.layout.fragment_test_server);
		//myTextView = (TextView)findViewById(R.id.myTextView3);
		//myTextView.setText(message);
		//myTextView.append(message+"\n");
	
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
			View rootView = inflater.inflate(R.layout.fragment_test_server,
					container, false);
			return rootView;
		}
	}

	private Runnable startserver = new Thread(){
		ServerSocket serverSocket = null;
        boolean listening = true;
        
        
        public void run() {
			
        try {
            serverSocket = new ServerSocket(port);
        } catch (IOException e) {
            System.err.println("Could not listen on specified port.");
            System.exit(-1);
        }
        Socket client = null;
        
	        while (listening)
	        	
	        {
	        	try
	        	{
	        		//serverSocket.setSoTimeout(TIMEOUT*1000);
	        		client = serverSocket.accept();
	        		new initializeConnection(client).start();
	        	}
	        	catch (Exception e)
		        {
	        		if (client != null)
	        		{
		        	e.printStackTrace();
		        	//if (client != null) client.close();
	        		}
		        }
	        }
	     
        }
	};
	
	public class initializeConnection extends Thread  {
		private Socket socket = null;
		public initializeConnection(Socket socket) {
			super("initializeConnection");
			this.socket = socket;
		    }
		
		public void run() {
			
			String message2="!!in thread, run func is called!!\n";
			Log.d("WifiTestServer", message2);
			//setContentView(R.layout.fragment_test_server);
			//myTextView = (TextView)findViewById(R.id.myTextView3);
			//myTextView.append(message2);

			
		    Log.d("WifiTestServer", "client is initialized");
		    // initialize server socket
		    try{
		    	
		     //server.setSoTimeout(TIMEOUT*1000);
			   
		       //attempt to accept a connection
		       
		    	   
		          BufferedReader br = new BufferedReader(new InputStreamReader(socket.getInputStream()));  
		       
		          StringBuilder sb = new StringBuilder();  
		          Writer writer = new OutputStreamWriter(socket.getOutputStream()); 
		          String temp;  
		          int index;  
		          SocketProtocol sp = new SocketProtocol();
		          
		          String outputLine = sp.processInput(wifiAdmin, null);
		          writer.write(outputLine);
		          writer.flush();
		          while ((temp = br.readLine()) != null) {  
		             //Log.d("WifiTestServer", temp); 
		             /*
		             if ((index = temp.indexOf("eof")) != -1) {
		              sb.append(temp.substring(0, index));  
		                 break;  
		                 */
		             outputLine = sp.processInput(wifiAdmin, temp);
		             if (outputLine.equals("EOF")) break;
		             writer.write(outputLine);
		             writer.flush();
			          //writer.write("eof\n");
		               
		             //sb.append(temp);  
		          }  
		          
		          //String rcv_msg = "received from client:" + sb;
		          //myTextView.append(rcv_msg);
		          //Log.d("WifiTestServer", rcv_msg); 
		          
		            
		            if(socket != null)
		            {
			          writer.close();  
			          br.close();  
			          socket.close();
		            }
		    
		    //socketOut = new PrintWriter(client.getOutputStream(), true);
		    } catch (SocketTimeoutException e) {
		    // print out TIMEOUT
		     connectionStatus="Connection has timed out! Please try again";
		     mHandler.post(showConnectionStatus);
		    } catch (Exception e) {
		     Log.e(TAG, ""+e);
		    } finally {
			 Log.d("WifiTestServer", "!!! finally, close socket!");
		    //close the server socket
			 //if (socket != null){
		    //socket.close();}
		    //}

			
		    
			     
		
		
		}
		
	
		
		};
	
	
		private Runnable showConnectionStatus = new Runnable() {
			public void run() {
			    Toast.makeText(TestServerActivity.this, connectionStatus, Toast.LENGTH_SHORT).show();
			}
		};
			
	
	
}


class Task implements Runnable {  
	   
    private Socket socket;  
      
    public Task(Socket socket) {  
       this.socket = socket;  
    }  
      
    public void run() {  
       try {  
          handleSocket();  
       } catch (Exception e) {  
          e.printStackTrace();  
       }  
    }  
      
    private void handleSocket() throws Exception {  
  	  
        BufferedReader br = new BufferedReader(new InputStreamReader(socket.getInputStream()));  
        StringBuilder sb = new StringBuilder();  
        String temp;  
        int index;  
        while ((temp=br.readLine()) != null) {  
           Log.d("WifiTestServer", temp);  
           if ((index = temp.indexOf("eof")) != -1) {
            sb.append(temp.substring(0, index));  
               break;  
           }  
           sb.append(temp);  
        }  
        Log.d("WifiTestServer", "from client: " + sb);  
    
        Writer writer = new OutputStreamWriter(socket.getOutputStream());  
        writer.write("Hello Client.");  
        writer.write("eof\n");  
        writer.flush();  
        writer.close();  
        br.close();  
        socket.close();  
  	  
  	  
    }
    
}

}




