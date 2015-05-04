using System;
using System.Collections.Generic;
using System.Linq;

namespace PADIMapNoReduce
{
	[Serializable]
	public class Job {
		public Guid Uuid;
		public List<string> workers = new List<string>();
		public List<int> splitsCompleted = new List<int>();
		public List<string> takeoverOrder = new List<string>();

		public int nSplits;
		public int inputSize;

		public string mapperName;
		public byte[] mapperCode;

		public string clientAddress;
		public string coordinatorAddress;

		public Job(string coordinatorAddress, string clientAddress, int inputSize, int nSplits, string mapperName, byte[] mapperCode) {
			Uuid = System.Guid.NewGuid ();
			this.coordinatorAddress = coordinatorAddress;
			this.clientAddress = clientAddress;
			this.inputSize = inputSize;
			this.nSplits = nSplits;
			this.mapperName = mapperName;
			this.mapperCode = mapperCode;
		}

		public void splitCompleted(int split) {
			splitsCompleted.Add (split);
			if(splitsCompleted.Count>nSplits) {
				// trigger job completed
			}
		}

		/// <summary>
		/// Generates splits.
		/// Takes into account splits completed.
		/// </summary>
		/// <returns>The splits.</returns>
		public List<Split> generateSplits() {
			var temp = new List<int> ();
			int chunk = inputSize / nSplits;

			int rest = inputSize % nSplits;
			for (var i = 0; i < nSplits; i++) {
				int toAdd = chunk;
				if (rest > i) {
					toAdd += 1;
				}
				temp.Add(toAdd);
			}

			var result = new List<Split> ();
			result.Add (new Split(Uuid, 0, 0, temp[0]));
			for(int i=1; i<temp.Count; i++){
				int lowerBound = result[i-1].upper;
				int higherBound = lowerBound + temp[i];
				result.Add (new Split (Uuid, i, lowerBound, higherBound));
			}

			return result.Where((elm, index) => !splitsCompleted.Contains (index)).ToList();
		}

		public override string ToString() {
			return Uuid.ToString ();
		}
	}

	[Serializable]
	public class Split {
		public int id;
		public Guid jobUuid;
		public int lower;
		public int upper;
		//public List<string> values;

		public Split(Guid jobUuid, int id, int lower, int upper) {
			this.jobUuid = jobUuid;
			this.id = id;
			this.lower = lower;
			this.upper = upper;
		}

		/*public void addValue(string value) {
			values.Add (value);
		}*/

		public override string ToString() {
			return jobUuid.ToString ()+"#"+id;
		}
	}
}

