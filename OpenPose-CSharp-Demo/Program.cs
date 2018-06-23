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

		// Broadcast listener
		private BroadcastProxy broadcastProxy;

		public static PoseEventListener listener;

		static void Main(string[] args)
		{
			Program program = new Program();
		}

		public Program()
		{
			// Try connecting by default
			ConnectOrReconnect();

			listener = new PoseEventListener(this);
			KeyPoint2D.X_Scale_PPM = 426.6f;
			KeyPoint2D.Y_Scale_PPM = 447.2f;

			OpenPose_Reader.Model = Model.MPI;
			OpenPose_Reader2D.Simulate3D = true;

			OpenPose_Reader2D reader = new OpenPose_Reader2D("C:\\Users\\Dankrushen\\openpose-1.3.0-win64-gpu-binaries\\JSON_Output\\", listener)
			{
				QueueCheckDelay = 3
			};

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

		public PoseEventListener(Program program)
		{
			this.program = program;
		}

		public void OnPoseGenerated(Pose pose)
		{
			//Console.WriteLine("Test");

			foreach (KeyPoint point in pose.KeyPoints)
			{
				Console.WriteLine(point.ToString());
			}

			if (pose != null && typeof(Pose2D) == pose.GetType())
			{
				Pose2D pose2D = (Pose2D)pose;

				KeyPoint2D nose = pose2D.GetKeyPoint2D(BodyPoint.Nose_or_Top_Head);
				//KeyPoint2D leftEye = pose2D.GetKeyPoint2D(BodyPoint.Left_Eye);
				//KeyPoint2D rightEye = pose2D.GetKeyPoint2D(BodyPoint.Right_Eye);

				if (nose != null && nose.IsValid)//&& leftEye != null && rightEye != null && nose.IsValid && leftEye.IsValid && rightEye.IsValid)
				{
					//if (!HeadsetStatus)
					//{
					//	program.headTrackingService.ChangeStatus(true);
					//	HeadsetStatus = true;
					//}

					//float height = (leftEye.Y + rightEye.Y) / 2;
					program.headTrackingService.SendPositionOnly(nose.X, nose.Y, 0);
				}
				//else
				//{
				//	if (HeadsetStatus)
				//	{
				//		program.headTrackingService.ChangeStatus(false);
				//		HeadsetStatus = false;
				//	}
				//}

				KeyPoint2D rightHand = pose2D.GetKeyPoint2D(BodyPoint.Right_Wrist);

				if (rightHand != null && rightHand.IsValid)
				{
					program.controllerService.SetControllerState(0, rightHand.X, rightHand.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
				}

				KeyPoint2D leftHand = pose2D.GetKeyPoint2D(BodyPoint.Left_Wrist);

				if (leftHand != null && leftHand.IsValid)
				{
					program.controllerService2.SetControllerState(1, leftHand.X, leftHand.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
				}
			}
			else if (pose != null && typeof(Pose3D) == pose.GetType())
			{
				Pose3D pose3D = (Pose3D)pose;

				KeyPoint3D nose = pose3D.GetKeyPoint3D(BodyPoint.Nose_or_Top_Head);

				if (nose != null && nose.IsValid)
				{
					program.headTrackingService.SendRotationAndPosition(0, 0, 0, nose.X, nose.Y, 0);
				}

				KeyPoint3D rightHand = pose3D.GetKeyPoint3D(BodyPoint.Right_Wrist);

				if (rightHand != null && rightHand.IsValid)
				{
					program.controllerService.SetControllerState(0, rightHand.X, rightHand.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
				}

				KeyPoint3D leftHand = pose3D.GetKeyPoint3D(BodyPoint.Left_Wrist);

				if (leftHand != null && leftHand.IsValid)
				{
					program.controllerService2.SetControllerState(1, leftHand.X, leftHand.Y, leftHand.Z, 0, 0, 0, 0, 0, 0, false, false, false);
				}
			}
		}
	}
}
