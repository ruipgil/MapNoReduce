using System;
using System.Collections.Generic;

namespace PADIMapNoReduce
{
	[Serializable]
	public class StatusInfo
	{
		public string workerAddress;
		public List<string> knownWorkers;
		public decimal load;
		public int tracking;

		public StatusInfo (string workerAddress, List<string> knownWorkers, decimal load, int tracking)
		{
			this.workerAddress = workerAddress;
			this.knownWorkers = knownWorkers;
			this.load = load;
			this.tracking = tracking;
		}
	}
}

