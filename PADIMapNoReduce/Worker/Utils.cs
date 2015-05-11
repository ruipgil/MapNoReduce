using System;

namespace PADIMapNoReduce
{
	public class Utils
	{
		public static double Elapsed(DateTime start) {
			return (DateTime.Now - start).TotalMilliseconds;
		}
	}
}

