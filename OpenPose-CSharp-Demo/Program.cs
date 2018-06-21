using OpenPose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPose_CSharp_Demo
{
	public class Program
	{
		public static PoseEventListener listener = new PoseEventListener();

		static void Main(string[] args)
		{
			OpenPose_Reader reader = new OpenPose_Reader("C:\\Users\\Dankrushen\\openpose-1.3.0-win64-gpu-binaries\\JSON_Output\\", listener);

			reader.AsynchronousQueueReader().Join();
		}
	}

	public class PoseEventListener : IPoseEvent
	{
		public void OnPoseGenerated(Pose2D pose)
		{
			//Console.WriteLine("Test");

			foreach (KeyPoint2D keypoint in pose.KeyPoints)
			{
				Console.WriteLine(keypoint.ToString());
			}
		}
	}
}
