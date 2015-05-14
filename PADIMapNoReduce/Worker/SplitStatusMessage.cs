using System;

namespace PADIMapNoReduce
{
	[Serializable]
	public struct SplitStatusMessage
	{
		public WorkStatus status;
		public int remaining;

		public SplitStatusMessage(WorkInfo info) {
			status = info.status;
			remaining = info.remaining;
		}

		public SplitStatusMessage(WorkStatus status) {
			this.status = status;
			remaining = 0;
		}
	}
}

