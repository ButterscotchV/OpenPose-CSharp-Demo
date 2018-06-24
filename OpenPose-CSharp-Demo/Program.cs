using OpenPose;
using OpenPose.Pose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VRE.Vridge.API.Client.Messages.Control;
using VRE.Vridge.API.Client.Messages.v3.Broadcast;
using VRE.Vridge.API.Client.Proxy;
using VRE.Vridge.API.Client.Proxy.Broadcasts;
using VRE.Vridge.API.Client.Proxy.Controller;
using VRE.Vridge.API.Client.Proxy.HeadTracking;

namespace OpenPose_CSharp_Demo
{
	public class Program
	{
		// API client         
		private readonly APIClient apiClient = new APIClient("OpenPose CSharp Demo");

		// Helper services
		public TrackingService headTrackingService;
		public ControllerService controllerService;
		public ControllerService controllerService2;
		//public ControllerService controllerService3;
		//public ControllerService controllerService4;

		// Broadcast listener
		private BroadcastProxy broadcastProxy;

		public PoseEventListener listener;
		public TrackingInterpolation tracking;

		static void Main(string[] args)
		{
			Program program = new Program();
		}

		public Program()
		{
			// Try connecting by default
			ConnectOrReconnect();

			listener = new PoseEventListener(this);

			tracking = new TrackingInterpolation(this, listener);

			KeyPoint2D.X_Scale_PPM = 426.6;
			KeyPoint2D.Y_Scale_PPM = 447.2;

			KeyPoint3D.X_Scale_PPM = 426.6;
			KeyPoint3D.Y_Scale_PPM = 447.2;

			OpenPose_Reader.Model = Model.COCO;
			OpenPose_Reader2D.Simulate3D = true;

			OpenPose_Reader2D reader = new OpenPose_Reader2D("C:\\Users\\Dankrushen\\openpose-1.3.0-win64-gpu-binaries\\JSON_Output\\", listener)
			{
				QueueCheckDelay = 5
			};

			tracking.RunAsyncInterpolator();

			reader.QueueReader();
		}

		private void ConnectOrReconnect()
		{
			try
			{
				// Connect to the services
				headTrackingService = new TrackingService(apiClient.CreateProxy<HeadTrackingProxy>());

				controllerService = new ControllerService(apiClient.CreateProxy<ControllerProxy>());
				controllerService2 = new ControllerService(apiClient.CreateProxy<ControllerProxy>());
				//controllerService3 = new ControllerService(apiClient.CreateProxy<ControllerProxy>());
				//controllerService4 = new ControllerService(apiClient.CreateProxy<ControllerProxy>());

				broadcastProxy = apiClient.CreateProxy<BroadcastProxy>();

				headTrackingService.ChangeStatus(true);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private string CheckStatus()
		{
			var status = apiClient.GetStatus();
			if (status == null)
			{
				return null;
			}

			var msg = "API endpoint status: ";
			status.Endpoints.ForEach(e => msg += $"{e.Name} is {(ControlResponseCode)e.Code}, ");
			return msg;
		}

		private void ResetAsyncRotation()
		{
			headTrackingService.ResetAsyncOffset();
		}

		private void RecenterView()
		{
			headTrackingService.RecenterView();
		}
	}

	public class PoseEventListener : IPoseEvent
	{
		private Program program;
		//private static bool HeadsetStatus = false;

		private double LastTime = ConvertToUnixTimestampMs(DateTime.UtcNow);
		public double TimeBetweenPoses = 0;

		public Pose lastPose;
		public Pose curPose;
		public Pose newPose;

		public PoseEventListener(Program program)
		{
			this.program = program;
		}

		public void OnPoseGenerated(Pose pose)
		{
			lastPose = curPose;
			newPose = pose;

			double curTime = ConvertToUnixTimestampMs(DateTime.UtcNow);
			TimeBetweenPoses = curTime - LastTime;
			LastTime = curTime;
		}

		public static DateTime ConvertFromUnixTimestampMs(double timestamp)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return origin.AddMilliseconds(timestamp);
		}

		public static double ConvertToUnixTimestampMs(DateTime date)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan diff = date.ToUniversalTime() - origin;
			return diff.TotalMilliseconds;
		}
	}
}
