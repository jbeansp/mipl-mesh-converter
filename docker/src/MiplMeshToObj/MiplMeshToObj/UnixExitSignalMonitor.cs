//using System;
//using System.Threading;
//using Mono.Unix;

//namespace MiplMeshToObj
//{
//	public static class UnixExitSignalMonitor
//	{
//		public static event EventHandler cancelEvent;


//		//static constructor
//		static UnixExitSignalMonitor()
//		{
//			UnixSignal[] signals = new UnixSignal[] { };
//			try
//			{
//				//Docker fires SIGTERM then SIGKILL to the container.  
//				//SIGKILL can't be ignored or caught.  It throws an exception if I include it here.
//				signals = new UnixSignal[] {
//					new UnixSignal (Mono.Unix.Native.Signum.SIGTERM),
//					//new UnixSignal (Mono.Unix.Native.Signum.SIGKILL), 
//					//new UnixSignal (Mono.Unix.Native.Signum.SIGINT),
//					//new UnixSignal (Mono.Unix.Native.Signum.SIGHUP),
//					};
//			}
//			catch (Exception e)
//			{
//				Logger.Log("Caught exception: {0}", e);
//			}



//			Thread signal_thread = new Thread(delegate ()
//			{
//				while (true)
//				{
//					// Wait for a signal to be delivered
//					int index = UnixSignal.WaitAny(signals, -1);

//					Mono.Unix.Native.Signum signal = signals[index].Signum;

//					// Notify the main thread that a signal was received,
//					// you can use things like:
//					//    Application.Invoke () for Gtk#
//					//    Control.Invoke on Windows.Forms
//					//    Write to a pipe created with UnixPipes for server apps.
//					//    Use an AutoResetEvent

//					if (cancelEvent != null)
//					{
//						cancelEvent(null, EventArgs.Empty);
//						return;
//					}
						
//					//// For example, this works with Gtk#
//					//Application.Invoke(delegate () { ReceivedSignal(signal); });
//				}
//			});

//			signal_thread.Start();
//		}
//	}

//}
