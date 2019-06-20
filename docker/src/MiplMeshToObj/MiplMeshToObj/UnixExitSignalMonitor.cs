using System;
using System.Threading.Tasks;
using Mono.Unix;

namespace MiplMeshToObj
{
	public class UnixExitSignalMonitor
	{
		public event EventHandler cancelEvent;


		public UnixExitSignalMonitor()
		{
			UnixSignal[] signals = new UnixSignal[] { };
			try
			{
				//Docker fires SIGTERM then SIGKILL to the container.  
				//SIGKILL can't be ignored or caught.  It throws an exception if I include it here.
				signals = new UnixSignal[] {
					new UnixSignal (Mono.Unix.Native.Signum.SIGTERM),
					//new UnixSignal (Mono.Unix.Native.Signum.SIGKILL), 
					//new UnixSignal (Mono.Unix.Native.Signum.SIGINT),
					//new UnixSignal (Mono.Unix.Native.Signum.SIGHUP),
					};
			}
			catch (Exception e)
			{
				Logger.Log("Caught exception: {0}", e);
			}


			Task.Run(
				() =>
				{
					while (true)
					{
						// Wait for a signal to be delivered
						int index = UnixSignal.WaitAny(signals, -1);

						//Mono.Unix.Native.Signum signal = signals[index].Signum;

						if (cancelEvent != null)
						{
							cancelEvent(null, EventArgs.Empty);
						}

						return;
					}
				});

		}
	}

}
