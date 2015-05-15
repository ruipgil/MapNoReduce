using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace PADIMapNoReduce
{
	public abstract class Worker : WorkerKnowledge
	{
		Dictionary<string, WorkInfo> instanceLoad = new Dictionary<string, WorkInfo> ();
		HashSet<string> splitsDone = new HashSet<string>();
		ManualResetEvent freezeW_ = new ManualResetEvent(true);
		int slow = 0;

		public Worker (int port) : base(port) {}
		public Worker (string address, int port) : base(address, port) {}

		private delegate IList<KeyValuePair<string, string>> MapFn(string line);

		public void cancelSplit(Guid job, int split) {
			var id = Split.CreateID(job, split);
			if (instanceLoad.ContainsKey (id)) {
				Console.WriteLine ("[Split]X "+id);
				instanceLoad[id].cancel = true;
			}
		}

		public virtual int getLoad () {
			return instanceLoad.Values.Sum(x=>x.remaining) / Const.N_PARALLEL_MAP_PER_JOB;
		}

		public SplitStatusMessage getSplitStatus(Guid jobId, int splitId) {
			freezeW_.WaitOne ();

			string id = Split.CreateID(jobId, splitId);
			if (splitsDone.Contains (id)) {
				return new SplitStatusMessage(WorkStatus.Done);
			}
			if(instanceLoad.ContainsKey(id)) {
				return new SplitStatusMessage(instanceLoad [id]);
			}
			return new SplitStatusMessage(WorkStatus.Inexistent);
		}

		public bool work(Split split) {
			freezeW_.WaitOne();

			if (instanceLoad.ContainsKey (split.ToString ())) {
				Console.WriteLine ("[Split]Join "+split);
				instanceLoad[split.ToString()].thread.Join();
				return true;
			}

			Console.WriteLine ("[Split]> "+split);

			WorkInfo winfo = new WorkInfo(split, Thread.CurrentThread);
			instanceLoad.Add (split.ToString(), winfo);

			MapFn map;
			try	{
				map = buildMapperInstance(split);
			} catch (Exception e) {
				Console.WriteLine(e);
				return false;
			}

			var results = new ConcurrentQueue<IList<KeyValuePair<string, string>>> ();

			freezeW_.WaitOne();
			var client = (IClientService)Activator.GetObject (typeof(IClientService), split.Client);
			var lines = client.get(split.lower, split.upper);

			if (winfo.cancel) {
				instanceLoad.Remove (split.ToString());
				return false;
			}

			winfo.status = WorkStatus.Mapping;

			Parallel.ForEach(lines, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Const.N_PARALLEL_MAP_PER_JOB, winfo.remaining) }, (line, _)=>{
				if (winfo.cancel) {
					instanceLoad.Remove (split.ToString());
					_.Stop();
					return;
				}

				freezeW_.WaitOne();

				if(slow!=0) {
					Thread.Sleep(slow);
					slow = 0;
				}

				results.Enqueue(map (line));
				winfo.remaining--;
			});

			/*if( winfo.remaining < halfWayPoint && replicated ) {
				string worker;
				getWorker(worker).sendReplicatedData(results.ToList(), split, winfo.remaining);
				split.Coordinator.informReplication(worker, split, winfo.remaining);
			}*/

			if (winfo.cancel) {
				instanceLoad.Remove (split.ToString());
				return false;
			}

			freezeW_.WaitOne();
			winfo.status = WorkStatus.Sending;

			try {
				client.set(split.id, results.ToList());
				freezeW_.WaitOne();
				/*string worker;
				getWorker(worker).dropReplicatedData(results.ToList(), split, winfo.remaining);*/
			} catch(Exception e) {
				Console.WriteLine (e);
			}

			instanceLoad.Remove (split.ToString());
			splitsDone.Add (split.ToString());

			Console.WriteLine ("[Split]<  "+split+" in "+Utils.Elapsed(winfo.started)+"ms");

			freezeW_.WaitOne();
			Async.each (split.Trackers, (tracker) => {
				freezeW_.WaitOne();
				Console.WriteLine ("[Split]I  "+split+" to "+tracker);
				try {
					var w = (IWService)Activator.GetObject(typeof(IWService), tracker);
					w.completedSplit (split.jobUuid, split.id);
				} catch(Exception) {
					removeWorkers(tracker);
				}
			});
			return true;
		}

		private MapFn buildMapperInstance(Split split) {
			Assembly assembly = Assembly.Load(split.mapperCode);
			foreach (Type type in assembly.GetTypes()) {
				if (type.IsClass == true) {
					if (type.FullName.EndsWith("." + split.mapperName)) {
						object ClassObj = Activator.CreateInstance(type);
						MapFn callback = (line)=> {
							object[] args = new object[] { line };
							object resultObject = type.InvokeMember("Map", BindingFlags.Default | BindingFlags.InvokeMethod, null, ClassObj, args);
							return (IList<KeyValuePair<string, string>>) resultObject;
						};
						return callback;
					}
				}
			}
			return null;
		}

		public void slowWorker(int seconds)
		{
			slow = seconds * 1000;
		}

		public void freezeWorker()
		{
			Console.WriteLine("FREEZING W");
			freezeW_.Reset();
		}

		public void unfreezeWorker()
		{
			Console.WriteLine("UNFREEZING W");
			freezeW_.Set();
		}

		public virtual void getStatus() {
			string str = "";
			foreach(var work in instanceLoad) {

				switch (work.Value.status)
				{
				case WorkStatus.Getting:
					str += "\t" + work.Key + " is getting data";
					break;
				case WorkStatus.Sending:
					str += "\t" + work.Key + " is sending all processed data";
					break;
				default:
					str += "\t" + work.Key + " is mapping, " + work.Value.remaining + " keys remaining";
					break;
				}
				str += " "+work.Value.remaining+" \n";
			}
			Console.WriteLine (str);
		}
	}
}

