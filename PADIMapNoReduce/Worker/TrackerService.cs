using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PADIMapNoReduce
{

	public class TrackerService : MarshalByRefObject, IWorkerService, IWService
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
		HashSet<string> knownWorkers = new HashSet<string> ();
		Dictionary<string, IWService> workersInstances = new Dictionary<string, IWService>();
		Dictionary<string, WorkInfo> instanceLoad = new Dictionary<string, WorkInfo> ();
		HashSet<string> splitsDone = new HashSet<string>();

		const int MAX_TRANSFER_MB = 1;
		const int N_PARALLEL = 1;
		const int N_PARALLEL_MAP_PER_JOB = 8;
		const int LOAD = 10000;
		const int TRACKER_OVERHEAD_VS_WORKER = 100;
		const double WORK_TIME_THRESHOLD = 60 * 1000; // 60secs

		public string ownAddress;
        
        private int slow = 0;
		private bool freezeW = false;
		private bool freezeC = false;

		public TrackerService (string myAddress, int port)
		{
			this.ownAddress = myAddress+":"+port+"/W";

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			RemotingServices.Marshal(this, "W", typeof(TrackerService));

			//knownWorkers.Add (ownAddress);
			workersInstances.Add (ownAddress, this);

			Console.WriteLine("Worker created at '"+ownAddress+"'");
		}

		public TrackerService(int port) : this("tcp://localhost", port) {
		}

        public override object InitializeLifetimeService()
        {
            return null;
        }

		public void addKnownWorkers(List<string> workers) {
			workers.Remove (ownAddress);
			workers.ForEach (w=>knownWorkers.Add(w));

			currentJobs.Values.Where (job=>{
				return job.Trackers.Count == 0;
			}).ToList().ForEach(assignReplica);
		}

		/// <summary>
		/// Gets a worker instance based on its address.
		/// </summary>
		/// <returns>The worker.</returns>
		/// <param name="address">Address.</param>
		public IWService getWorker(string address) {
			while (freezeC) {
			}
			//Console.WriteLine ("Getting worker "+address);
			if(workersInstances.ContainsKey (address)) {
				if(address.Equals(ownAddress)) {
					return this;
				}
				return workersInstances [address];
			} else {
				// instantiate worker
				IWService worker = (IWService)Activator.GetObject(typeof(IWService), address);
				workersInstances.Add(address, worker);
				knownWorkers.Add (address);
				return worker;
			}
		}

		public void heartbeat(string workerAddress) {
			while (freezeC) {
			}

			//Console.WriteLine (" # Received heartbeat of "+workerAddress);
			if (!knownWorkers.Contains (workerAddress)) {
				knownWorkers.Add (workerAddress);
			}
		}

		public void shareKnownWorkers(string sender, HashSet<string> workers) {
			while (freezeC) {
			}

			workers.Remove (ownAddress);
			workers.Add (sender);
			foreach(var worker in workers) {
				knownWorkers.Add (worker);
			}
		}

		public void startHeartbeating() {
			while (freezeC) {
			}
			
			List<string> toHeartbeat = new List<string> ();
			foreach (var job in currentJobs.Values) {
				toHeartbeat.Add (job.Coordinator);
				toHeartbeat.AddRange (job.Trackers);
			}
			toHeartbeat = toHeartbeat.Distinct ().ToList ();
			toHeartbeat.Remove (ownAddress);

			//Console.WriteLine (" # Heartbeat ["+toHeartbeat.Count+"] "+string.Join (" ", toHeartbeat.ToArray()));
			if (toHeartbeat.Count < 1) {
				return;
			}

			var workersToDealWith = new List<string> ();
			Async.eachBlocking (toHeartbeat, (worker)=>{
				try {
					getWorker(worker).heartbeat(ownAddress);
				} catch(Exception) {
					//Console.WriteLine ("# A worker is down: "+worker);
					workersToDealWith.Add (worker);
				}
			});
			// different process
			removeWorkers (workersToDealWith);
		}

		public void startSharingKnownWorkers() {
			while (freezeC) {
			}

			Async.eachBlocking (knownWorkers.ToList(), (worker)=> {
				try {
					getWorker(worker).shareKnownWorkers(ownAddress, knownWorkers);
				} catch(Exception) {}
			});
		}

		/// <summary>
		/// Removes workers from the known worker list.
		/// It will check if any of these workers are coordinator. If any of them is, it will check
		/// if this instance is next in line to take over the coordination of a job.
		/// </summary>
		/// <param name="workers">Workers.</param>
		public void removeWorkers(List<string> workers) {

			workers.Remove (ownAddress);
			workers.ForEach (w=>knownWorkers.Remove(w));

			var toTakeControl = currentJobs.Values.Where (job=>{
				return workers.Contains(job.Coordinator) &&
					job.Trackers.Except(knownWorkers).First().Equals(ownAddress);
			}).ToList();

			foreach (var job in toTakeControl) {
				takeOwnershipOfJob (job);
			}

			var toCreateReplica = currentJobs.Values.Where (job=>{
				return !job.Trackers.Except (workers).Any ();
			}).ToList();

			foreach (var job in toCreateReplica) {
				assignReplica (job);
			}

			foreach (var job in currentJobs.Values) {
				workers.ForEach (job.removeAssignmentsFromWorker);
			}
		}

		public void removeWorkers(string worker) {
			removeWorkers (new List<string> (){ worker });
		}

		public void assignReplica(Job job) {
			var workers = getWorkersByLoad ();
			workers.Remove (ownAddress);
			job.Trackers = workers.GetRange(0, Math.Min(1, workers.Count));
			if (job.Trackers.Count > 0) {
				Async.eachBlocking (job.Trackers.ToList (), (tracker) => {
					Console.WriteLine ("[Job]Update " + job + " to " + tracker);
					try {
						getWorker (tracker).announceJob (job);
					} catch(Exception) {
						removeWorkers(tracker);
					}
				});
			}
		}

		/// <summary>
		/// Changes the job so that this tracker will be the coordinator.
		/// Updates the job in all workers.
		/// </summary>
		/// <param name="jobUuid">Job id.</param>
		public void takeOwnershipOfJob(Job job) {
			Console.WriteLine ("[Job]Taking ownership of "+job);
			Console.WriteLine ("\n"+job.debugDump ());

			var otherTrackers = job.Trackers;
			otherTrackers.Remove (ownAddress);
			job.Coordinator = ownAddress;

			foreach (var assignment in job.Assignments.ToDictionary(a=>a.Key, a=>a.Value)) {
				var status = WorkStatus.Inexistent;
				try {
					status = getWorker(assignment.Value).getSplitStatus (job.Uuid, assignment.Key);
				} catch(Exception) {
					removeWorkers (assignment.Value);
				}
				if (status == WorkStatus.Done || status == WorkStatus.Inexistent) {
					job.Assignments.Remove (assignment.Key);
					if (status == WorkStatus.Done) {
						job.splitCompleted (assignment.Key);
					}
				}
			}

			assignReplica (job);

			Async.ExecInThread(() => startJob (job));
		}

		public void completedSplit(Guid job, int split) {
			while (freezeC) {
			}

			var splitId = job + "#" + split;
			Console.WriteLine ("[Split]C "+splitId);
			if (currentJobs.ContainsKey (job)) {
				currentJobs [job].splitCompleted (split);
				currentJobs [job].deassign (split);
			}
		}

		public void completedJob(Guid job) {
			while (freezeC) {
			}

			Console.WriteLine ("[Job]C " + job);
			currentJobs.Remove (job);
		}

		public void announceJob(Job job) {
			while (freezeC) {
			}

			Console.WriteLine ("[Job]Announced "+job);
			Console.WriteLine ("\n"+job.debugDump ());
			if (job.Trackers.Contains (ownAddress)) {
				if (currentJobs.ContainsKey (job.Uuid)) {
					currentJobs [job.Uuid] = job;
				} else {
					currentJobs.Add (job.Uuid, job);
				}
			} else if (currentJobs.ContainsKey (job.Uuid)) {
				currentJobs.Remove (job.Uuid);
			}
		}

		public void submit(string clientAddress, int inputSize, long fileSize,  int splits, byte[] code, string mapperName) {
			while (freezeC) {
			}
            
			Console.WriteLine ("[Submit]\n\tClient: "+clientAddress+"\n\tLines: "+inputSize+"\n\tSize: "+fileSize+" Bytes\n\tMapper Name: "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			assignReplica (job);

			Async.ExecInThread(() => startJob (job));
		}

		public List<string> getWorkersByLoad() {
			var workers = new List<Tuple<string, int>> ();
			Async.eachBlocking (knownWorkers.ToList (), (worker) => {
				try {
					workers.Add (new Tuple<string, int> (worker, getWorker (worker).getLoad ()));
				} catch(Exception) {
					removeWorkers(worker);
				}
			});
			workers.Add (new Tuple<string, int>(ownAddress,getLoad()));

			return workers.OrderByDescending (x => x.Item2).Select(x=>x.Item1).ToList();
		}

		public void assignSplit(Guid jobId, int splitId, string worker) {
			while (freezeC) {
			}

			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.assign (splitId, worker);
			}
		}

		public void deassignSplit(Guid jobId, int split) {
			while (freezeC) {
			}

			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.deassign (split);
			}
		}

		public void informReplicas(Job job, Action<string> action) {
			while (freezeC) {
			}

			if( job.hasReplicas() ) {
				Async.eachBlocking (job.Trackers, (w)=>{
					try {
						action(w);
					 } catch (Exception) {
						removeWorkers (w);
					}
				});
			}
		}

		public void startJob(Job job) {
			Console.WriteLine ("[Job]> "+job);

			List<Split> splits;
			do {
				splits = job.generateSplits ();

				var wList = new Queue<string>(getWorkersByLoad ());
				Object workLock = new object();
				Parallel.ForEach (splits, new ParallelOptions { MaxDegreeOfParallelism = wList.Count }, (s, _, index) => {

					string worker;
					lock(workLock) {
						try {
							worker = wList.Dequeue();
						} catch( Exception ) {
							return;
						}
					}
					//Split s = splits.Dequeue ();
					Console.WriteLine ("[Job]I " + s + " to " + worker);
					try {
						job.assign (s.id, worker);
						var w = getWorker (worker);
						informReplicas (job, (tracker) => {
							getWorker (tracker).assignSplit (job.Uuid, s.id, worker);
						});
						w.work (s);

						completedSplit (job.Uuid, s.id);
						lock(workLock) {
							wList.Enqueue(worker);
						}
					} catch (Exception) {
						lock(workLock) {
							removeWorkers (worker);

							informReplicas (job, (tracker) => {
								getWorker (tracker).deassignSplit (job.Uuid, s.id);
							});
						}
					}
				});
			} while(splits.Count > 0 || job.Assignments.Count > 0);

			informReplicas (job, (tracker) => {
				Console.WriteLine ("[Job]I(T) " + job + " to " + tracker);
				getWorker (tracker).completedJob (job.Uuid);
			});

			var client = (IClientService)Activator.GetObject (typeof(IClientService), job.Client);
			client.done ();

			Console.WriteLine (job.debugDump());
			Console.WriteLine ("[Job]< "+job);

			currentJobs.Remove (job.Uuid);
		}

		public WorkStatus getSplitStatus(Guid jobId, int splitId) {
			string id = jobId + "#" + splitId;
			if (splitsDone.Contains (id)) {
				return WorkStatus.Done;
			}
			if(instanceLoad.ContainsKey(id)) {
				return instanceLoad [id].status;
			}
			return WorkStatus.Inexistent;
		}

		private delegate IList<KeyValuePair<string, string>> MapFn(string line);

		private MapFn buildMapperInstance(Split split) {
			Assembly assembly = Assembly.Load(split.mapperCode);
			// Walk through each type in the assembly looking for our class
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

		public void work(Split split) {

			if (instanceLoad.ContainsKey (split.ToString ())) {
				Console.WriteLine ("[Split]Join "+split);
				instanceLoad[split.ToString()].thread.Join();
				return;
			}


            Console.WriteLine ("[Split]> "+split);

			WorkInfo winfo = new WorkInfo();
			winfo.started = DateTime.Now;
			winfo.split = split;
			winfo.remaining = split.upper - split.lower;
			winfo.thread = Thread.CurrentThread;
            winfo.status = WorkStatus.Getting;
			instanceLoad.Add (split.ToString(), winfo);

			MapFn map = buildMapperInstance(split);

			var results = new ConcurrentQueue<IList<KeyValuePair<string, string>>> ();

			var client = (IClientService)Activator.GetObject (typeof(IClientService), split.Client);
			var lines = client.get(split.lower, split.upper);

			winfo.status = WorkStatus.Mapping;
            
			Async.eachLimitBlocking(lines, (line)=>{
				if(slow!=0) {
					Thread.Sleep(slow);
					slow = 0;
				}
				results.Enqueue(map (line));
				winfo.remaining--;
			}, Math.Min(N_PARALLEL_MAP_PER_JOB, winfo.remaining));

            winfo.status = WorkStatus.Sending;
			// data replication:
			//  - start sending client and at the same time send copy to trackers
			//  - when ends sending to client signals trackes to delete info.
			try {
				client.set(split.id, results.ToList());
			} catch(Exception e) {
				Console.WriteLine ("Exception on sending data to client");
				Console.WriteLine (e);
			}

			instanceLoad.Remove (split.ToString());
			splitsDone.Add (split.ToString());

			Console.WriteLine ("[Split]<  "+split+" in "+Utils.Elapsed(winfo.started)+"ms");

			Async.each (split.Trackers, (tracker) => {
				Console.WriteLine ("[Split]I  "+split+" to "+tracker);
				try {
					var w = (IWService)Activator.GetObject(typeof(IWService), tracker);
					w.completedSplit (split.jobUuid, split.id);
				} catch(Exception) {
					removeWorkers(tracker);
				}
			});
		}

		public int getLoad () {
            var s = (slow == 0 ? 1 : (slow / 1000));
			return s * currentJobs.Values.Count * TRACKER_OVERHEAD_VS_WORKER + instanceLoad.Values.Sum(x=>x.remaining) * s / N_PARALLEL_MAP_PER_JOB;
		}

        public void slowWorker(int seconds)
        {
            slow = seconds * 1000;
        }

		public void freezeWorker()
		{
			if (freezeW) {
				return;
			} else {
				freezeW = true;
			}
			foreach (var work in instanceLoad.Values) {
				work.thread.Interrupt ();
			}
		}

		public void unfreezeWorker()
		{
			if (!freezeW) {
				freezeW = false;
			} else {
				return;
			}
			foreach (var work in instanceLoad.Values) {
				new Thread (() => work.thread.Join ());
			}
		}

		public void freezeCoordinator()
		{
			freezeC = true;
		}

		public void unfreezeCoordinator()
		{
			freezeC = false;
		}
        
        public void getStatus()
        {
            int nTracking = currentJobs.Values.Where(x=>x.Coordinator.Equals(ownAddress)).Count();
            int nReplicas = currentJobs.Values.Where(x => x.Trackers.Contains(ownAddress)).Count();
            string str = "=============\n";
            str+="This worker is the coordinator of "+nTracking+" jobs and is the replica of "+nReplicas+".\n";
            str += "Job list:\n";
            foreach (var job in currentJobs.Values)
            {
                str += job.debugDump();
            }
            str += "This worker known about: " + string.Join(", ", knownWorkers.ToArray())+"\n";
            str += "Current works:\n";
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
                str += "\n";
            }
            str += "Current load: " + getLoad() + "\n";
            str += "=============\n";

            Console.WriteLine (str);
        }
	}
}

