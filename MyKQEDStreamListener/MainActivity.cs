using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Media;

namespace MyKQEDStreamListener {
	[Activity (Label = "MyKQEDStreamListener", MainLauncher = true, Theme = "@android:style/Theme.Holo")]
	public class MainActivity : Activity {
	
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			VolumeControlStream = Stream.Music;

			SetContentView (Resource.Layout.Main);

			Button btnPlay = FindViewById<Button> (Resource.Id.btnPlay);
			btnPlay.Click += delegate
			{
				var intent = new Intent(StreamService.ActionPlay);
				StartService(intent);
			};

			var btnStop = FindViewById<Button> (Resource.Id.btnStop);
			btnStop.Click += delegate
			{
				var intent = new Intent(StreamService.ActionStop);
				StartService(intent);
			};

			var btnPause = FindViewById<Button> (Resource.Id.btnPause);
			btnPause.Click += delegate
			{
				var intent = new Intent(StreamService.ActionPause);
				StartService(intent);
			};
		}

		protected override void OnResume ()
		{
			base.OnResume ();
		}
		protected override void OnPause ()
		{
			base.OnPause ();
		}
		protected override void OnStop ()
		{
			base.OnStop ();
		}

		//override On
			
	}
}


