using System;
using System.Threading;

namespace PADIMapNoReduce
{
	public class WorkInfo {
		public Split split;
		public Thread thread;
		public int remaining;
		public WorkStatus status;
		public DateTime started;
		public bool cancel;

		public WorkInfo(Split split, Thread current) {
			cancel = false;
			started = DateTime.Now;
			this.split = split;
			remaining = split.upper - split.lower;
			thread = Thread.CurrentThread;
			status = WorkStatus.Getting;
		}
	}
}

