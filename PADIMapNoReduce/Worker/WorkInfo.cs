using System;
using System.Threading;

namespace PADIMapNoReduce
{
	public struct WorkInfo {
		public Split split;
		public Thread thread;
		public int remaining;
		public WorkStatus status;
		public DateTime started;
	}
}

