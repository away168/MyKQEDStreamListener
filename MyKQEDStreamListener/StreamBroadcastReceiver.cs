﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MyKQEDStreamListener {

	[BroadcastReceiver]
	[IntentFilter(new string[] {Intent.ActionMediaButton})]
	public class MediaButtonEventReceiver : BroadcastReceiver {
		public override void OnReceive (Context context, Intent intent)
		{
			Intent command = null; 
			if (Intent.ActionMediaButton.Equals(intent.Action))
			{
				var keypress = intent.Extras.Get(Intent.ExtraKeyEvent) as KeyEvent;
				switch (keypress.KeyCode)
				{
				case Keycode.MediaPlay:
				case Keycode.MediaPlayPause:
					command = new Intent (StreamService.ActionPlay);
					break;
				case Keycode.MediaStop:
				case Keycode.MediaPause:
					command = new Intent (StreamService.ActionPause);
					break;
				}
				if (command != null)
					context.StartService (command);
			}
			Toast.MakeText (context, String.Format("Received intent! {0}", intent.ToString()), ToastLength.Short).Show ();
		}
	}
}
