using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System.Threading.Tasks;
using Android.Net.Wifi;


namespace MyKQEDStreamListener 
{
	// TODO: and handle interruptions properly. 
	[Service]
	[IntentFilter(new string[] { ActionPlay, ActionPause, ActionStop })]
	public class StreamService : Service, AudioManager.IOnAudioFocusChangeListener {

		enum MediaStates 
		{
			Zero,
			Idle,
			Initialized,
			Preparing,
			Prepared, 
			Started,
			Stopped,
			Paused,
			PlaybackCompleted,
			End,
			Error
		};

		enum ServiceActions
		{
			Play,
			Pause,
			Stop
		}

		MediaStates currentState = MediaStates.Zero;
		const int NotificationID = 1;
		public const string ActionPlay = "net.wayfamily.action.PLAY";
		public const string ActionPause = "net.wayfamily.action.PAUSE";
		public const string ActionStop = "net.wayfamily.action.STOP";
	
		const string OGKQEDSource = @"http://streams.kqed.org/kqedradio.m3u";
		const string KQEDSOURCE = @"http://streams2.kqed.org:80/kqedradio";

		int lastStartId;

		AudioManager am;
		WifiManager.WifiLock wifiLock;
		MediaPlayer player;

		// State Machine!
		async Task HandleAction(ServiceActions action)
		{
			if (action == ServiceActions.Play)
			{
				switch (currentState)
				{
				case MediaStates.End:
				case MediaStates.Error:
				case MediaStates.Zero:
					await initializePlayer ();
					break;
				case MediaStates.Idle:
					await player.SetDataSourceAsync (ApplicationContext, Android.Net.Uri.Parse (KQEDSOURCE));
					currentState = MediaStates.Initialized;
					await HandleAction (ServiceActions.Play);
					break;
				case MediaStates.Initialized:
					player.PrepareAsync ();
					currentState = MediaStates.Preparing;
					break;
				case MediaStates.Preparing:
					// nothing to do. waiting for return.
					break;
				case MediaStates.Prepared:
					Start ();
					break;
				case MediaStates.Started:
					// do nothing - its already playing
					break;
				case MediaStates.Paused:
					Start ();
					break;
				case MediaStates.Stopped:
					player.PrepareAsync ();
					currentState = MediaStates.Preparing;
					break;
				
				}
			}
			else if (action == ServiceActions.Pause)
			{
				switch (currentState)
				{
				case MediaStates.Started:
					Pause ();
					break;
				default:
					//ignore
					break;
				}
			}
			else if (action == ServiceActions.Stop)
			{
				switch (currentState)
				{
				case MediaStates.Prepared:
				case MediaStates.Started:
				case MediaStates.Paused:
				case MediaStates.Stopped:
				case MediaStates.PlaybackCompleted:
					Stop ();
					break;
				default:
					currentState = MediaStates.Error;
					break;
				}
			}
		}

		/// <summary>
		/// Instantiate's a new MediaPlayer
		/// and configures the handlers (Prepared / Error)
		/// </summary>
		async Task initializePlayer ()
		{
			player = new MediaPlayer ();
			player.SetAudioStreamType (Stream.Music);
			player.SetWakeMode (ApplicationContext, WakeLockFlags.Partial); // this keeps the player alive when the screen goes to sleep. 

			player.Prepared += async (sender, e) =>
			{
				currentState = MediaStates.Prepared;
				await HandleAction (ServiceActions.Play);
			};

			player.Error += (sender, e) =>
			{
				currentState = MediaStates.Error;
				player.Release ();
				player.Dispose ();
				Console.WriteLine ("StreamService : MediaPlayer Error {0}", e.What);
			};
			currentState = MediaStates.Idle;
			await HandleAction (ServiceActions.Play);
		}

		// stop the player if we lose focus / play if we get it back
		void AudioManager.IOnAudioFocusChangeListener.OnAudioFocusChange (AudioFocus focusChange)
		{
			switch (focusChange)
			{
			case AudioFocus.Loss:
				HandleAction (ServiceActions.Pause);
				break;
			case AudioFocus.Gain:
				HandleAction (ServiceActions.Play);
				break;
			case AudioFocus.LossTransientCanDuck:
				// lower volume
				break;
			case AudioFocus.GainTransientMayDuck:
				// bring back volume
				break;
			}
		}

		// TODO: Open M3U, parse and save it

		public override void OnCreate ()
		{
			base.OnCreate ();

			currentState = MediaStates.Zero;
			am = GetSystemService (Context.AudioService) as AudioManager;
			wifiLock = ((WifiManager)GetSystemService (Context.WifiService)).CreateWifiLock (Android.Net.WifiMode.Full, "myServiceLock");
			//am.RegisterMediaButtonEventReceiver (MyKQEDStreamReceiver);
		}

		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
		{
			lastStartId = startId;
			switch (intent.Action) {
			case ActionPlay: HandleAction(ServiceActions.Play); break;
			case ActionStop: HandleAction(ServiceActions.Stop); break;
			case ActionPause:
				HandleAction (ServiceActions.Pause); break;
			}

			return StartCommandResult.Sticky;
		}

		// not required for our usage
		public override IBinder OnBind (Intent intent)
		{
			return null;
		}
			
		public override bool StopService (Intent name)
		{
			//StopHelper ();
			return base.StopService (name);
		}

		public override void OnDestroy ()
		{
			StopHelper ();
			base.OnDestroy ();
		}

		// As a "music" player | must run as a Foreground Service - 
		//			http://developer.android.com/guide/topics/media/mediaplayer.html
		//StartForeground(NotificationID, )
		//			String songName;
		//			// assign the song name to songName
		//			PendingIntent pi = PendingIntent.getActivity(getApplicationContext(), 0,
		//				new Intent(getApplicationContext(), MainActivity.class),
		//				PendingIntent.FLAG_UPDATE_CURRENT);
		//			Notification notification = new Notification();
		//			notification.tickerText = text;
		//			notification.icon = R.drawable.play0;
		//			notification.flags |= Notification.FLAG_ONGOING_EVENT;
		//			notification.setLatestEventInfo(getApplicationContext(), "MusicPlayerSample",
		//				"Playing: " + songName, pi);
		//			startForeground(NOTIFICATION_ID, notification);

		Notification buildNotification()
		{
			PendingIntent pi = PendingIntent.GetActivity (ApplicationContext, 0, new Intent (ApplicationContext, typeof(MainActivity)), PendingIntentFlags.CancelCurrent);
			//PendingIntent pi = PendingIntent.GetActivity (ApplicationContext, 0, new Intent (ActionPlay), PendingIntentFlags.NoCreate);
			var notification = new Notification (Android.Resource.Drawable.IcMediaPlay, "KQED LiveStream") {
				Flags = NotificationFlags.OngoingEvent,
			};
			notification.SetLatestEventInfo (ApplicationContext, "KQED LiveStream", "TODO: GetCurrentStream", pi);

			return notification;
		}

		Notification buildCustomNotification(ServiceActions action)
		{
			PendingIntent pi = PendingIntent.GetActivity (ApplicationContext, 0, new Intent (ApplicationContext, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent);
			PendingIntent play = PendingIntent.GetService (ApplicationContext, 1, new Intent (ActionPlay), PendingIntentFlags.CancelCurrent);
			PendingIntent pause = PendingIntent.GetService (ApplicationContext, 2, new Intent (ActionPause), PendingIntentFlags.CancelCurrent);
			Android.App.Notification.Builder builder = new Notification.Builder (ApplicationContext);

			var myRemoteView = new RemoteViews (PackageName, Resource.Layout.Notification);

			builder.SetContent (myRemoteView);
			// if play
			if (action == ServiceActions.Play)
				builder.SetSmallIcon (Android.Resource.Drawable.IcMediaPlay);
			else if (action == ServiceActions.Pause)
				builder.SetSmallIcon (Android.Resource.Drawable.IcMediaPause);
				
			myRemoteView.SetOnClickPendingIntent (Resource.Id.notifyBtnPause, pause);
			myRemoteView.SetOnClickPendingIntent (Resource.Id.notifyBtnPlay, play);
			myRemoteView.SetOnClickPendingIntent (Resource.Id.notifyTitle, pi);
			myRemoteView.SetImageViewResource (Resource.Id.notifyImage, Resource.Drawable.KQED);
			myRemoteView.SetTextViewText (Resource.Id.notifyTitle, "KQED 88.5FM LiveStream");

			return builder.Notification;
		}

		//Set sticky as we are a long running operation
		void StartForeground ()
		{
			StartForeground (NotificationID, buildCustomNotification (ServiceActions.Play));
		}

		void Start()
		{
			player.Start ();
			am.RequestAudioFocus (this, Stream.Music, AudioFocus.Gain);
			wifiLock.Acquire ();
			StartForeground (); //makes the service stay alive.
			currentState = MediaStates.Started;
		}

		void Pause()
		{
			player.Pause ();
			am.AbandonAudioFocus (this);

//			PendingIntent pi = PendingIntent.GetActivity (ApplicationContext, 0, new Intent (ApplicationContext, typeof(MainActivity)), PendingIntentFlags.CancelCurrent);
//			var notification = new Notification (Android.Resource.Drawable.IcMediaPause, "KQED LiveStream") {
//				Flags = NotificationFlags.OngoingEvent,
//			};
//
//			notification.SetLatestEventInfo (ApplicationContext, "KQED LiveStream", "TODO: GetCurrentStream", pi);
//
			using ( var notificationManager = GetSystemService (Context.NotificationService) as NotificationManager)
			{
//				notificationManager.Notify (NotificationID, notification);
				notificationManager.Notify (NotificationID, buildCustomNotification(ServiceActions.Pause));
			}

			currentState = MediaStates.Paused;
		}

		void Stop()
		{
			StopSelf (lastStartId);
			//am.UnregisterMediaButtonEventReceiver ();
			//am.UnregisterRemoteControlClient ();
		}

		void StopHelper ()
		{
			currentState = MediaStates.End;

			if (player != null) {
				player.Stop ();
				player.Release ();
				player = null;
			}
			wifiLock.Release ();
			StopForeground (true);
		}
	}
}

