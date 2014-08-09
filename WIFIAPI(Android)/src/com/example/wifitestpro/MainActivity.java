package com.example.wifitestpro;

import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.support.v4.app.Fragment;
import android.support.v7.app.ActionBarActivity;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.Toast;
import android.widget.ToggleButton;
import android.os.Process;

public class MainActivity extends ActionBarActivity {

	public final static String EXTRA_MESSAGE = "com.example.myfirstapp.MESSAGE";
	
	private static boolean isExit = false; 
	private ToggleButton toggleButton; 
	
	//private static final String TAG = MainActivity_Exit.class.getSimpleName(); 
	
	
	private static Handler mHandler = new Handler() {  
		  
        @Override  
        public void handleMessage(Message msg) {  
            super.handleMessage(msg);  
            isExit = false;  
        }  
    };  
	
	
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);

		if (savedInstanceState == null) {
			getSupportFragmentManager().beginTransaction()
					.add(R.id.container, new PlaceholderFragment()).commit();
		}
		System.out.println("!!!! server started!");
        Intent intent = new Intent(this, TestServerActivity.class);
        String message = "start";
        intent.putExtra(EXTRA_MESSAGE, message);
        startActivity(intent);
			
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {

		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.main, menu);
		return true;
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
			View rootView = inflater.inflate(R.layout.fragment_main, container,
					false);
			return rootView;
		}
	}
 
	public void sendMessage(View view) {
		
		Intent intent = new Intent(this, DisplayMessageActivity.class);
		EditText editText = (EditText) findViewById(R.id.edit_message);
		String message = editText.getText().toString();
		intent.putExtra(EXTRA_MESSAGE, message);
		startActivity(intent);
		
	}
	
	public void onToggleClicked(View view) {
		   
	    boolean on = ((ToggleButton) view).isChecked();
	    
	    if (on) {
	    	//stop server
	        System.out.println("!!!! server stopped!");
	        Intent intent = new Intent(this, TestServerActivity.class);
	        String message = "stop";
	        intent.putExtra(EXTRA_MESSAGE, message);
	        startActivity(intent);
	        
	    } else {
	    	//start server 
	        System.out.println("!!!! server started!");
	        Intent intent = new Intent(this, TestServerActivity.class);
	        String message = "start";
	        intent.putExtra(EXTRA_MESSAGE, message);
	        startActivity(intent);
	    }
	}
	
	
	 public boolean onKeyDown(int keyCode, KeyEvent event) {  
	        if (keyCode == KeyEvent.KEYCODE_BACK) {  
	            exit();  
	            return true;  
	        }  
	        return super.onKeyDown(keyCode, event);  
	    }  
	
	 private void exit() {  
	        if (!isExit) {  
	            isExit = true;  
	            Toast.makeText(getApplicationContext(), "click again to exit!",  
	                    Toast.LENGTH_SHORT).show();  
	            
	            mHandler.sendEmptyMessageDelayed(0, 2000);  
	        } else {  
	            
	            //Log.e(TAG, "exit application");  
	            System.out.println("exit application");  
	            //this.finish();  
	            android.os.Process.killProcess(android.os.Process.myPid());
	            System.exit(0);
	        }  
	    }  

}





