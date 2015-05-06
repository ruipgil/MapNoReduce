using System;
using System.Collections.Generic;
using System.Linq;

namespace PADIMapNoReduce
{
	[Serializable]
	public class Job {
		private Guid uuid;

		private int nSplits;
		private int inputSize;
		private int inputSizeBytes;
		private List<int> splitsCompleted = new List<int>();

		private string mapperName;
		private byte[] mapperCode;

		private string clientAddress;
		private string coordinatorAddress;
		private List<string> trackers = new List<string>();

		public Job(string coordinatorAddress, string clientAddress, int inputSize, int nSplits, string mapperName, byte[] mapperCode) {
			uuid = System.Guid.NewGuid ();
			this.coordinatorAddress = coordinatorAddress;
			this.clientAddress = clientAddress;
			this.inputSize = inputSize;
			this.nSplits = nSplits;
			this.mapperName = mapperName;
			this.mapperCode = mapperCode;
		}

		public Guid Uuid { get { return uuid; } }

		public List<string> Trackers {
			get { return trackers; }
			set { trackers = value; }
		}
		public string Coordinator { 
			get { return coordinatorAddress; }
			set { coordinatorAddress = value; }
		}

		public int NSplits { get { return nSplits; } }
		public int InputSize { get { return inputSize; } }
		public int InputSizeBytes { get { return InputSizeBytes; } }

		public string MapperName { get { return mapperName; } }
		public byte[] MapperCode { get { return mapperCode; } }

		public string Client { get { return clientAddress; } }

		public bool hasReplicas() {
			return trackers.Count > 0;
		}

		public bool splitCompleted(int split) {
			splitsCompleted.Add (split);
			return splitsCompleted.Count>nSplits;
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
			result.Add (new Split(this, 0, 0, temp[0]));
			for(int i=1; i<temp.Count; i++){
				int lowerBound = result[i-1].upper;
				int higherBound = lowerBound + temp[i];
				result.Add (new Split (this, i, lowerBound, higherBound));
			}

			return result.Where((elm, index) => !splitsCompleted.Contains (index)).ToList();
		}

		public override string ToString() {
			return uuid.ToString ();
		}

		public string debugDump() {
			var str = "Job " + this + "\n" +
			          "nSplits: " + nSplits + "\n" +
			          "splitsCompleted: ";
			foreach (var s in splitsCompleted) {
				str += s + ", ";
			}
			str += "\n";
			str += "client: " + clientAddress + "\n" +
				"coordinator: " + coordinatorAddress + "\n" +
				"trackers: ";
			foreach (var t in trackers) {
				str += t + ", ";
			}
			str += "\n";

			return str;
		}
	}

	[Serializable]
	public class Split {
		public int id;
		public Job Job;
		public int lower;
		public int upper;
		//public List<string> values;

		public Split(Job jobUuid, int id, int lower, int upper) {
			this.Job = jobUuid;
			this.id = id;
			this.lower = lower;
			this.upper = upper;
		}

		/*public void addValue(string value) {
			values.Add (value);
		}*/

		public override string ToString() {
			return Job.Uuid.ToString ()+"#"+id;
		}
	}
}

