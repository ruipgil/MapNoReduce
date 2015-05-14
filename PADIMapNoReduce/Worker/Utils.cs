using System;

namespace PADIMapNoReduce
{
	public class Utils
	{
		public static double Elapsed(DateTime start) {
			return Elapsed(DateTime.Now, start);
		}
		public static double Elapsed(DateTime now, DateTime start) {
			return (now - start).TotalMilliseconds;
		}
	}
}

