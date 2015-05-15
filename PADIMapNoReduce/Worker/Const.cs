using System;

namespace PADIMapNoReduce
{
	public class Const {
		public const int TRACKER_OVERHEAD_VS_WORKER = 100;
		public const int N_PARALLEL_MAP_PER_JOB = 8;
		public const int FILE_SIZE_THRESHOLD_MB = 5 * 1000 * 1000;
		public const long SPLIT_FILE_SIZE = 1 * 1000 * 1000;
		public const double SPLIT_HEALTHY_PROGRESS = 0.1;
		public const int SPLIT_MONITORING_TIME_MS = 2000;
	}
}

