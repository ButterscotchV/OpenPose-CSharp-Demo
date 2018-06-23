using System;
using System.Windows.Media.Media3D;

namespace OpenPose_CSharp_Demo
{
	public static class Helpers
	{
		public static Quaternion QuaternionFromYawPitchRoll(float yaw, float pitch, float roll)
		{
			double angleSquared = pitch * pitch + yaw * yaw + roll * roll;
			double s = 0;
			double c = 1;
			if (angleSquared > 0)
			{
				double angle = Math.Sqrt(angleSquared);
				s = Math.Sin(angle * 0.5f) / angle;
				c = Math.Cos(angle * 0.5f);
			}

			return new Quaternion(s * pitch, s * yaw, s * roll, c);
		}

		public static float[] DoubleArraytoFloatArray(double[] array)
		{
			float[] newArray = new float[array.Length];

			for (int i = 0; i < array.Length; i++)
			{
				newArray[i] = (float)array[i];
			}

			return newArray;
		}
	}
}
