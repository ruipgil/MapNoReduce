using System;
using System.Collections.Generic;

namespace PADIMapNoReduce
{
	[Serializable]
	public class Split {
		public int id;
		public byte[] mapperCode;
		public string mapperName;
		public Guid jobUuid;
		public string Client;
		public List<string> Trackers;

		public int lower;
		public int upper;

		public Split(Job job, int id, int lower, int upper) {
			this.jobUuid = job.Uuid;
			this.mapperCode = job.MapperCode;
			this.mapperName = job.MapperName;
			this.Client = job.Client;
			this.Trackers = new List<string>(job.Trackers);
			this.id = id;
			this.lower = lower;
			this.upper = upper;
		}

		public override string ToString() {
			return CreateID(jobUuid, id);
		}

		public static string CreateID(Guid id, int split) {
			return id.ToString () + "#" + split;
		}
	}
}

