using System;
using System.Collections.Generic;
using System.Linq;

namespace PADIMapNoReduce
{
	[Serializable]
	public class Job {
		private Guid uuid;
		private DateTime started;

		private int nSplits;
		private int inputSize;
		private long fileSize;
		private HashSet<int> splitsCompleted = new HashSet<int>();

		private string mapperName;
		private byte[] mapperCode;

		private string clientAddress;
		private string coordinatorAddress;
		private List<string> trackers = new List<string>();

		private Dictionary<int, string> assignments = new Dictionary<int, string>();

		public Job(string coordinatorAddress, string clientAddress, int inputSize, long fileSize, int nSplits, string mapperName, byte[] mapperCode) {
			uuid = System.Guid.NewGuid ();
			started = DateTime.Now;
			this.coordinatorAddress = coordinatorAddress;
			this.clientAddress = clientAddress;
			this.inputSize = inputSize;
			this.nSplits = nSplits;
			this.mapperName = mapperName;
			this.mapperCode = mapperCode;
			this.fileSize = fileSize;
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
		public int SplitsCompleted { get { return splitsCompleted.Count; } }
		public int InputSize { get { return inputSize; } }
		public long InputSizeBytes { get { return fileSize; } }

		public string MapperName { get { return mapperName; } }
		public byte[] MapperCode { get { return mapperCode; } }

		public string Client { get { return clientAddress; } }

		public Dictionary<int, string> Assignments { get { return assignments; } }

		public bool assign(int split, string worker) {
			if (assignments.ContainsKey (split)) {
				return false;
			} else {
				assignments.Add (split, worker);
				return true;
			}
		}

		public bool deassign(int split) {
			if (assignments.ContainsKey (split)) {
				assignments.Remove (split);
				return true;
			} else {
				return false;
			}
		}

		public void removeAssignmentsFromWorker(string worker) {
			var toRemove = new List<int> ();
			foreach (var assignment in assignments) {
				if (assignment.Value == worker) {
					toRemove.Add(assignment.Key);
				}
			}
			toRemove.ForEach (i => assignments.Remove (i));
		}

		public bool hasReplicas() {
			return trackers.Count > 0;
		}

		public bool splitCompleted(int split) {
			splitsCompleted.Add (split);
			assignments.Remove (split);
			return splitsCompleted.Count>nSplits;
		}

		/// <summary>
		/// Generates splits.
		/// Takes into account splits completed.
		/// </summary>
		/// <returns>The splits.</returns>
		public List<Split> generateSplits() {
			var THRESHOLD = 5 * 1000 * 1000;
			List<Split> result;
			if (InputSizeBytes >= THRESHOLD) {
				result = generateSplitsSizeBound ();
			} else {
				result = generateSplitsSplitBound ();
			}
			return result.Where((elm, index) => !splitsCompleted.Contains (index) && !assignments.ContainsKey(index)).ToList();
		}

		public List<Split> generateSplitsSplitBound() {
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

			return result;
		}

		public List<Split> generateSplitsSizeBound() {
			var totalLines = InputSize;
			var size = InputSizeBytes;
			var lines = InputSize;

			var averagePerLine = size / (float)totalLines;
			var predictedSize = lines * averagePerLine;

			long maxTransferSize = 1 * 1000 * 1000;

			var ns = Math.Ceiling(predictedSize/(double)maxTransferSize);
			var t = lines / (int)ns;
			var last = 0;
			var chunks = new List<Split> ();
			for (var i = 0; i < ns; i++) {
				var m = last + t;
				chunks.Add(new Split(this, i, last, m));
				last = m;
			}
			if (last != totalLines) {
				//Console.WriteLine ("not enough!");
				chunks.Last ().upper = totalLines;
				//chunks.Add (new Tuple<int, int>(s.lower, s.upper));
			}
			//chunks.ForEach (e => Console.WriteLine (e.lower+"-"+e.upper));
			return chunks;
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

			str += "splits assigned:\n";
			foreach (var a in assignments) {
				str += a.Key +" to " + a.Value + "\n";
			}

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

	public enum WorkStatus
	{
		Getting,
		Sending,
		Mapping,
		Inexistent,
		Done
	}
}

