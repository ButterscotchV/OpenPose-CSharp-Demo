using OpenPose.Pose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenPose_CSharp_Demo
{
	public class TrackingInterpolation
	{
		private Program program;
		private PoseEventListener eventListener;

		public static bool StopInterpolator = false;

		public int InterpolatorDelay = 10;
		public static double InterpolatorMultiplier = 1.5;

		private Pose lastPose;
		private int curPoseTimePassed = 0;

		public static int RotationAveragingAmount = 10;

		private Dictionary<BodyPoint, List<double>> yawAverage = new Dictionary<BodyPoint, List<double>>();
		private Dictionary<BodyPoint, List<double>> pitchAverage = new Dictionary<BodyPoint, List<double>>();
		private Dictionary<BodyPoint, List<double>> rollAverage = new Dictionary<BodyPoint, List<double>>();

		public List<Thread> interpolators = new List<Thread>();

		public TrackingInterpolation(Program program, PoseEventListener eventListener)
		{
			this.program = program;
			this.eventListener = eventListener;
		}

		public Thread RunAsyncInterpolator()
		{
			Thread thread = new Thread(new ThreadStart(AsyncPoseInterpolateLoop));
			thread.Start();

			interpolators.Add(thread);

			return thread;
		}

		public void AsyncPoseInterpolateLoop()
		{
			while (!StopInterpolator)
			{
				PoseInterpolate();
				Thread.Sleep(InterpolatorDelay);
				curPoseTimePassed += InterpolatorDelay;
			}
		}

		public void PoseInterpolate()
		{
			//Console.WriteLine("Test");

			if (eventListener.newPose == null || eventListener.TimeBetweenPoses == 0)
			{
				//Console.WriteLine("Error: Can't run interpolator...");
				return;
			}
			else if (eventListener.curPose == null || eventListener.lastPose == null)
			{
				eventListener.curPose = eventListener.newPose;
			}
			else // Interpolate
			{
				//Console.WriteLine("Interpolating pose...");

				if (lastPose != eventListener.lastPose)
				{
					curPoseTimePassed = 0;
					lastPose = eventListener.lastPose;
				}

				InterpolatorDelay = (int)Math.Floor(eventListener.TimeBetweenPoses / InterpolatorMultiplier);

				double interpolateDivider = (InterpolatorDelay > 0 ? ((eventListener.TimeBetweenPoses - curPoseTimePassed) / InterpolatorDelay) : 0);
				interpolateDivider = (interpolateDivider < 1 ? 1 : interpolateDivider);

				if (interpolateDivider >= 1 && InterpolatorDelay > 0)
				{
					//Console.WriteLine("Interpolate Divider: " + interpolateDivider);

					Pose newPose = eventListener.newPose;

					if (typeof(Pose2D) == newPose.GetType())
					{
						List<KeyPoint2D> newCurPose = new List<KeyPoint2D>();

						for (int i = 0; i < newPose.KeyPoints.Count; i++)
						{
							KeyPoint2D keyPoint = ((Pose2D)newPose).GetKeyPoint2D(i);

							KeyPoint2D newKeyPoint = InterpolateKeyPoint2Ds(((Pose2D)lastPose).GetKeyPoint2D(keyPoint.BodyPoint), keyPoint, interpolateDivider);

							if (newKeyPoint != null && newKeyPoint.IsValid)
							{
								newCurPose.Add(newKeyPoint);
							}
							else
							{
								newCurPose.Add(keyPoint);
							}
						}

						Pose2D interPose = new Pose2D(newCurPose.ToArray());

						foreach (KeyPoint2D keyPoint in newPose.KeyPoints)
						{
							if (interPose.GetKeyPoint2D(keyPoint.BodyPoint) == null)
							{
								interPose.KeyPoints.Add(keyPoint);
							}
						}

						eventListener.curPose = interPose;
					}
					else if (typeof(Pose3D) == newPose.GetType())
					{
						List<KeyPoint3D> newCurPose = new List<KeyPoint3D>();

						for (int i = 0; i < newPose.KeyPoints.Count; i++)
						{
							KeyPoint3D keyPoint = ((Pose3D)newPose).GetKeyPoint3D(i);

							KeyPoint3D newKeyPoint = InterpolateKeyPoint3Ds(((Pose3D)lastPose).GetKeyPoint3D(keyPoint.BodyPoint), keyPoint, interpolateDivider);

							if (newKeyPoint != null && newKeyPoint.IsValid)
							{
								newCurPose.Add(newKeyPoint);
							}
							else
							{
								newCurPose.Add(keyPoint);
							}
						}

						Pose3D interPose = new Pose3D(newCurPose.ToArray());

						foreach (KeyPoint3D keyPoint in newPose.KeyPoints)
						{
							if (interPose.GetKeyPoint3D(keyPoint.BodyPoint) == null)
							{
								interPose.KeyPoints.Add(keyPoint);
							}
						}

						eventListener.curPose = interPose;
					}
				}
			}

			//Console.WriteLine("Sending pose...");

			SendPose(eventListener.curPose);
		}

		public KeyPoint3D InterpolateKeyPoint3Ds(KeyPoint3D keyPoint, KeyPoint3D newKeyPoint, double interpolateAmount)
		{
			if (keyPoint != null && keyPoint.IsValid && newKeyPoint != null && newKeyPoint.IsValid)
			{
				// Differences
				double x_diff = (newKeyPoint.Raw_X - keyPoint.Raw_X) / interpolateAmount;
				double y_diff = (newKeyPoint.Raw_Y - keyPoint.Raw_Y) / interpolateAmount;
				double z_diff = (newKeyPoint.Raw_Z - keyPoint.Raw_Z) / interpolateAmount;

				double yaw_diff = (newKeyPoint.Yaw - keyPoint.Yaw) / interpolateAmount;
				double pitch_diff = (newKeyPoint.Pitch - keyPoint.Pitch) / interpolateAmount;
				double roll_diff = (newKeyPoint.Roll - keyPoint.Roll) / interpolateAmount;

				// New values
				double new_x = keyPoint.Raw_X + x_diff;
				double new_y = keyPoint.Raw_Y + y_diff;
				double new_z = keyPoint.Raw_Z + z_diff;

				double new_yaw = keyPoint.Yaw + yaw_diff;
				double new_pitch = keyPoint.Pitch + pitch_diff;
				double new_roll = keyPoint.Roll + roll_diff;

				//Console.WriteLine("Yaw: " + new_yaw);

				new_yaw = AverageYaw(newKeyPoint.BodyPoint, new_yaw);
				new_pitch = AveragePitch(newKeyPoint.BodyPoint, new_pitch);
				new_roll = AverageRoll(newKeyPoint.BodyPoint, new_roll);

				return new KeyPoint3D(newKeyPoint.BodyPoint, new_x, new_y, new_z, newKeyPoint.Score, new_yaw, new_pitch, new_roll);
			}

			return newKeyPoint;
		}

		public KeyPoint2D InterpolateKeyPoint2Ds(KeyPoint2D keyPoint, KeyPoint2D newKeyPoint, double interpolateAmount)
		{
			if (keyPoint != null && keyPoint.IsValid && newKeyPoint != null && newKeyPoint.IsValid)
			{
				// Differences
				double x_diff = (newKeyPoint.Raw_X - keyPoint.Raw_X) / interpolateAmount;
				double y_diff = (newKeyPoint.Raw_Y - keyPoint.Raw_Y) / interpolateAmount;

				double yaw_diff = (newKeyPoint.Yaw - keyPoint.Yaw) / interpolateAmount;
				double pitch_diff = (newKeyPoint.Pitch - keyPoint.Pitch) / interpolateAmount;
				double roll_diff = (newKeyPoint.Roll - keyPoint.Roll) / interpolateAmount;

				// New values
				double new_x = keyPoint.Raw_X + x_diff;
				double new_y = keyPoint.Raw_Y + y_diff;

				double new_yaw = keyPoint.Yaw + yaw_diff;
				double new_pitch = keyPoint.Pitch + pitch_diff;
				double new_roll = keyPoint.Roll + roll_diff;

				new_yaw = AverageYaw(newKeyPoint.BodyPoint, new_yaw);
				new_pitch = AveragePitch(newKeyPoint.BodyPoint, new_pitch);
				new_roll = AverageRoll(newKeyPoint.BodyPoint, new_roll);

				return new KeyPoint2D(newKeyPoint.BodyPoint, new_x, new_y, newKeyPoint.Score, new_yaw, new_pitch, new_roll);
			}

			return newKeyPoint;
		}

		public double AverageYaw(BodyPoint bodyPoint, double new_yaw)
		{
			if (yawAverage.ContainsKey(bodyPoint) && yawAverage.TryGetValue(bodyPoint, out List<double> yawValues))
			{
				yawValues.Add(new_yaw);

				if (yawValues.Count > RotationAveragingAmount)
				{
					for (int i = 0; i < yawValues.Count - RotationAveragingAmount; i++)
					{
						yawValues.RemoveAt(0);
					}
				}

				double averaged_yaw = 0;
				foreach (double yaw in yawValues)
				{
					averaged_yaw += yaw;
				}

				new_yaw = averaged_yaw / yawValues.Count;
			}
			else
			{
				yawValues = new List<double>
					{
						new_yaw
					};

				yawAverage.Add(bodyPoint, yawValues);
			}

			return new_yaw;
		}

		public double AveragePitch(BodyPoint bodyPoint, double new_pitch)
		{
			if (pitchAverage.ContainsKey(bodyPoint) && pitchAverage.TryGetValue(bodyPoint, out List<double> pitchValues))
			{
				pitchValues.Add(new_pitch);

				if (pitchValues.Count > RotationAveragingAmount)
				{
					for (int i = 0; i < pitchValues.Count - RotationAveragingAmount; i++)
					{
						pitchValues.RemoveAt(0);
					}
				}

				double averaged_pitch = 0;
				foreach (double pitch in pitchValues)
				{
					averaged_pitch += pitch;
				}

				new_pitch = averaged_pitch / pitchValues.Count;
			}
			else
			{
				pitchValues = new List<double>
					{
						new_pitch
					};

				pitchAverage.Add(bodyPoint, pitchValues);
			}

			return new_pitch;
		}

		public double AverageRoll(BodyPoint bodyPoint, double new_roll)
		{
			if (rollAverage.ContainsKey(bodyPoint) && rollAverage.TryGetValue(bodyPoint, out List<double> rollValues))
			{
				rollValues.Add(new_roll);

				if (rollValues.Count > RotationAveragingAmount)
				{
					for (int i = 0; i < rollValues.Count - RotationAveragingAmount; i++)
					{
						rollValues.RemoveAt(0);
					}
				}

				double averaged_roll = 0;
				foreach (double roll in rollValues)
				{
					averaged_roll += roll;
				}

				new_roll = averaged_roll / rollValues.Count;
			}
			else
			{
				rollValues = new List<double>
					{
						new_roll
					};

				rollAverage.Add(bodyPoint, rollValues);
			}

			return new_roll;
		}

		public void SendPose(Pose pose)
		{
			try
			{
				//foreach (KeyPoint point in pose.KeyPoints)
				//{
				//	Console.WriteLine(point.ToString());
				//}

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

						//double height = (leftEye.Y + rightEye.Y) / 2;
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
						program.controllerService2.SetControllerState(0, rightHand.X, rightHand.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
					}

					KeyPoint2D leftHand = pose2D.GetKeyPoint2D(BodyPoint.Left_Wrist);

					if (leftHand != null && leftHand.IsValid)
					{
						program.controllerService.SetControllerState(1, leftHand.X, leftHand.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
					}
				}
				else if (pose != null && typeof(Pose3D) == pose.GetType())
				{
					Pose3D pose3D = (Pose3D)pose;

					KeyPoint3D nose = pose3D.GetKeyPoint3D(BodyPoint.Nose_or_Top_Head);

					if (nose != null && nose.IsValid)
					{
						//program.headTrackingService.SendPositionOnly(nose.X, nose.Y, 0);
						program.headTrackingService.SendRotationAndPosition(nose.Yaw, nose.Pitch, nose.Roll, nose.X, nose.Y, nose.Z);
						//program.headTrackingService.SendAsyncOffset(0, 0, 0);
					}

					KeyPoint3D rightHand = pose3D.GetKeyPoint3D(BodyPoint.Right_Wrist);

					if (rightHand != null && rightHand.IsValid)
					{
						program.controllerService2.SetControllerState(0, rightHand.X, rightHand.Y, rightHand.Z, rightHand.Yaw, rightHand.Pitch, rightHand.Roll, 0, 0, 0, false, false, false);
					}

					KeyPoint3D leftHand = pose3D.GetKeyPoint3D(BodyPoint.Left_Wrist);

					if (leftHand != null && leftHand.IsValid)
					{
						program.controllerService.SetControllerState(1, leftHand.X, leftHand.Y, leftHand.Z, leftHand.Yaw, leftHand.Pitch, leftHand.Roll, 0, 0, 0, false, false, false);
					}

					KeyPoint3D rightFoot = pose3D.GetKeyPoint3D(BodyPoint.Right_Heel);

					if (rightFoot != null && rightFoot.IsValid)
					{
						//program.controllerService3.SetControllerState(2, rightFoot.X, rightFoot.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
					}

					KeyPoint3D leftFoot = pose3D.GetKeyPoint3D(BodyPoint.Left_Heel);

					if (leftFoot != null && leftFoot.IsValid)
					{
						//program.controllerService4.SetControllerState(3, leftFoot.X, leftFoot.Y, -0.6, 0, 0, 0, 0, 0, 0, false, false, false);
					}
				}
			}
			catch (Exception)
			{
				program.ConnectOrReconnect();
			}
		}
	}
}
