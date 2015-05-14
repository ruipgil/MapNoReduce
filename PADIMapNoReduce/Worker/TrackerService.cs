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

    public class TrackerService : MarshalByRefObject, IWorkerService, IWService, IRemoteTesting
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
        HashSet<Guid> jobsCompleted = new HashSet<Guid>();
		HashSet<string> knownWorkers = new HashSet<string> ();
		Dictionary<string, IWService> workersInstances = new Dictionary<string, IWService>();
		Dictionary<string, WorkInfo> instanceLoad = new Dictionary<string, WorkInfo> ();
		HashSet<string> splitsDone = new HashSet<string>();

		const int MAX_TRANSFER_MB = 1;
		const int N_PARALLEL = 1;
		const int N_PARALLEL_MAP_PER_JOB = 8;
		const int LOAD = 10000;
		const int TRACKER_OVERHEAD_VS_WORKER = 100;
		const double WORK_TIME_THRESHOLD = 3 * 1000; // 60secs
		const float WORK_PROGRESS_THRESHOLD = 0.01f;

		public string ownAddress;
        
        private int slow = 0;
		private bool freezeW = false;
        ManualResetEvent freezeC = new ManualResetEvent(true);
        ManualResetEvent freezeW_ = new ManualResetEvent(true);
		//private bool freezeC = false;
        TcpChannel channel;

		public TrackerService (string myAddress, int port)
		{
			this.ownAddress = myAddress+":"+port+"/W";

			channel = new TcpChannel(port);
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
            workers.ForEach(w =>
            {
                if (!knownWorkers.Contains(w))
                {
                    knownWorkers.Add(w);
                }
                else if(jobsCompleted.Count>0)
                {
                    // new worker or returnign one, send completed jobs
                    try
                    {
                        IWService worker = (IWService)Activator.GetObject(typeof(IWService), w);
                        worker.completedJobs(jobsCompleted);
                    }
                    catch (Exception) { }
                }
            });

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
            freezeC.WaitOne();

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
            freezeC.WaitOne();

			//Console.WriteLine (" # Received heartbeat of "+workerAddress);
			if (!knownWorkers.Contains (workerAddress)) {
				knownWorkers.Add (workerAddress);
			}
		}

		public void shareKnownWorkers(string sender, HashSet<string> workers) {
            freezeC.WaitOne();

            workers.Remove(ownAddress);
            workers.Add(sender);

            addKnownWorkers(workers.ToList());
		}

		public void startHeartbeating() {
            freezeC.WaitOne();
			
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
                    //Console.WriteLine("Heartbeating: " + worker);
					getWorker(worker).heartbeat(ownAddress);
                    //Console.WriteLine("Success: " + worker);
				} catch(Exception) {
					//Console.WriteLine ("# A worker is down: "+worker);
					workersToDealWith.Add (worker);
				}
			});
			// different process
			removeWorkers (workersToDealWith);
		}

		public void startSharingKnownWorkers() {
            freezeC.WaitOne();

			Async.eachBlocking (knownWorkers.ToList(), (worker)=> {
				try {
					getWorker(worker).shareKnownWorkers(ownAddress, knownWorkers);
				} catch(Exception) {}
			});
		}

		public void cancelSplit(Guid job, int split) {
			var id = job + "#" + split;
			if (instanceLoad.ContainsKey (id)) {
				Console.WriteLine ("canceling "+id);
				//instanceLoad [id].thread.Abort();
				instanceLoad[id].cancel = true;
				//instanceLoad.Remove (id);
			}
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
            freezeC.WaitOne();

			Console.WriteLine ("[Job]Taking ownership of "+job);
			Console.WriteLine ("\n"+job.debugDump ());

			var otherTrackers = job.Trackers;
			otherTrackers.Remove (ownAddress);
			job.Coordinator = ownAddress;

			foreach (var assignment in job.Assignments.ToDictionary(a=>a.Key, a=>a.Value)) {
				SplitStatusMessage status = new SplitStatusMessage (WorkStatus.Inexistent);
				try {
					status = getWorker(assignment.Value).getSplitStatus (job.Uuid, assignment.Key);
				} catch(Exception) {
					removeWorkers (assignment.Value);
				}
				if (status.status == WorkStatus.Done || status.status == WorkStatus.Inexistent) {
					job.Assignments.Remove (assignment.Key);
					if (status.status == WorkStatus.Done) {
						job.splitCompleted (assignment.Key);
					}
				}
			}

			assignReplica (job);

			Async.ExecInThread(() => startJob (job));
		}

		public void completedSplit(Guid job, int split) {
            freezeC.WaitOne();

			var splitId = job + "#" + split;
			Console.WriteLine ("[Split]C "+splitId);
			if (currentJobs.ContainsKey (job)) {
				currentJobs [job].splitCompleted (split);
				currentJobs [job].deassign (split);
			}
		}

		public void completedJob(Guid job) {
            freezeC.WaitOne();

			Console.WriteLine ("[Job]C " + job);
			currentJobs.Remove (job);
            jobsCompleted.Add(job);
		}

        public void completedJobs(HashSet<Guid> jobs)
        {
            foreach (var job in currentJobs.Keys.Intersect(jobs))
            {
                completedJob(job);
            }
        }

		public void announceJob(Job job) {
            freezeC.WaitOne();

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
            freezeC.WaitOne();
            
			Console.WriteLine ("[Submit]\n\tClient: "+clientAddress+"\n\tLines: "+inputSize+"\n\tSize: "+fileSize+" Bytes\n\tMapper Name: "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			Console.WriteLine ("assigning replica");
			assignReplica (job);

			Console.WriteLine ("starting job");

			Async.ExecInThread(() => startJob (job));
		}

		public List<string> getWorkersByLoad() {
            freezeC.WaitOne();
			var workers = new List<Tuple<string, int>> ();
			Async.eachBlocking (knownWorkers.ToList (), (worker) => {
				//Console.WriteLine(worker);
				try {
					workers.Add (new Tuple<string, int> (worker, getWorker (worker).getLoad ()));
					//Console.WriteLine("?");
				} catch(Exception) {
					//Console.WriteLine("?!!");
					removeWorkers(worker);
				}
			});
			//Console.WriteLine ("_---"+workers.Count);
			workers.Add (new Tuple<string, int>(ownAddress,getLoad()));
			//Console.WriteLine ("_---"+workers.Count);

			return workers.OrderByDescending (x => x.Item2).Select(x=>x.Item1).ToList();
		}

		public void assignSplit(Guid jobId, int splitId, string worker) {
            freezeC.WaitOne();

			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.assign (splitId, worker);
			}
		}

		public void deassignSplit(Guid jobId, int split) {
            freezeC.WaitOne();

			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.deassign (split);
			}
		}

		public void informReplicas(Job job, Action<string> action) {
            freezeC.WaitOne();

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
				var temp = getWorkersByLoad ();
				var wList = new Queue<string>(temp);
				var inParallels = Math.Min(wList.Count, splits.Count);

				if(inParallels==0){
					continue;
				}

				Parallel.ForEach (splits, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(wList.Count, splits.Count) }, (s, _, index) => {

					string worker;
					lock(wList) {
						try {
							worker = wList.Dequeue();
						} catch( Exception ) {
							return;
						}
					}
					//Split s = splits.Dequeue ();
					Console.WriteLine ("[Job]A " + s + " to " + worker);
					try {
						job.assign (s.id, worker);
						var w = getWorker (worker);
						informReplicas (job, (tracker) => {
							getWorker (tracker).assignSplit (job.Uuid, s.id, worker);
						});

						bool ps = false;
						SplitStatusMessage pastStatus = new SplitStatusMessage(WorkStatus.Inexistent);

						bool aborted = false;
						bool exception = false;
						var wt = new ThreadStart(()=>{
							try {
								w.work(s);
							} catch (Exception) {
								exception = true;
							}
						});
						var thr = new Thread(wt);

						var timer = new System.Timers.Timer(2000);
						timer.Elapsed += (source, e)=>{
							Console.WriteLine("-");
							var status = w.getSplitStatus(job.Uuid, s.id);
							if( ps ) {
								Console.WriteLine("Mark split! "+pastStatus.remaining+" "+status.remaining+" "+s);
								if( status.status == WorkStatus.Mapping && (pastStatus.remaining/(double)status.remaining)>0.1 ) {
									Console.WriteLine("Mark split! "+s+" "+_);
									deassignSplit(job.Uuid, s.id);
									w.cancelSplit(job.Uuid, s.id);
									informReplicas(job, (t)=>getWorker(t).deassignSplit(job.Uuid, s.id));
									timer.Enabled=false;
									aborted = true;
								}
							} else {
								ps = true;
								pastStatus = status;
							}
						};

						timer.Enabled = true;
						thr.Start();
						thr.Join();

						if( exception ) {
							//lock(wList) {
								removeWorkers (worker);

								informReplicas (job, (tracker) => {
									getWorker (tracker).deassignSplit (job.Uuid, s.id);
								});
							//}
						} else if( aborted ) {
							Console.WriteLine("Aborted "+s);
						} else {
							completedSplit (job.Uuid, s.id);
							lock(wList) {
								wList.Enqueue(worker);
							}
						}
						/*w.work (s);

						completedSplit (job.Uuid, s.id);
						lock(wList) {
							wList.Enqueue(worker);
						}*/
						timer.Enabled=false;
					} catch (Exception) {
						lock(wList) {
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

		public SplitStatusMessage getSplitStatus(Guid jobId, int splitId) {
			while (freezeW) {
			}

			string id = jobId + "#" + splitId;
			if (splitsDone.Contains (id)) {
				return new SplitStatusMessage(WorkStatus.Done);
			}
			if(instanceLoad.ContainsKey(id)) {
				return new SplitStatusMessage(instanceLoad [id]);
			}
			return new SplitStatusMessage(WorkStatus.Inexistent);
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
            freezeW_.WaitOne();

			if (instanceLoad.ContainsKey (split.ToString ())) {
				Console.WriteLine ("[Split]Join "+split);
				instanceLoad[split.ToString()].thread.Join();
				return;
			}

            Console.WriteLine ("[Split]> "+split);

			WorkInfo winfo = new WorkInfo(split, Thread.CurrentThread);
			instanceLoad.Add (split.ToString(), winfo);

			if (ownAddress.EndsWith ("30003/W")) {
				slow = 100;
			}

            MapFn map;
            try
            {
                map = buildMapperInstance(split);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

			var results = new ConcurrentQueue<IList<KeyValuePair<string, string>>> ();

            freezeW_.WaitOne();
			var client = (IClientService)Activator.GetObject (typeof(IClientService), split.Client);
			var lines = client.get(split.lower, split.upper);

			if (winfo.cancel) {
				instanceLoad.Remove (split.ToString());
				Console.WriteLine ("Canceled "+split);
				return;
			}

			winfo.status = WorkStatus.Mapping;
            
			Parallel.ForEach(lines, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(N_PARALLEL_MAP_PER_JOB, winfo.remaining) }, (line, _)=>{
				if (winfo.cancel) {
					instanceLoad.Remove (split.ToString());
					_.Stop();
					return;
				}

                freezeW_.WaitOne();

				if(slow!=0) {
					Thread.Sleep(slow);
					//slow = 0;
				}

				results.Enqueue(map (line));
				winfo.remaining--;
                //Console.WriteLine(winfo.remaining);
			});

			if (winfo.cancel) {
				instanceLoad.Remove (split.ToString());
				Console.WriteLine ("Canceled "+split);
				return;
			}

            freezeW_.WaitOne();
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
            Console.WriteLine("FREEZING W");
            freezeW_.Reset();
			/*if (freezeW) {
				return;
			} else {
				freezeW = true;
			}
			foreach (var work in instanceLoad.Values) {
                Console.WriteLine("FREEZING " + work.split);
                work.thread.Suspend();
			}*/
		}

		public void unfreezeWorker()
		{
            Console.WriteLine("UNFREEZING W");
			/*if (freezeW) {
				freezeW = false;
			} else {
				return;
			}
            Console.WriteLine("UNFREEZING W 2");
			foreach (var work in instanceLoad.Values) {
                Console.WriteLine("UNFREEZING "+work.split);
				//new Thread (() => work.thread.Join ());
                work.thread.Resume();
			}*/
            freezeW_.Set();
		}

		public void freezeCoordinator()
		{
            Console.WriteLine("FREEZING COORDINATOR");
			freezeC.Reset();
            //ChannelServices.UnregisterChannel(channel);
            RemotingServices.Marshal(this, "W", typeof(IRemoteTesting));
		}

		public void unfreezeCoordinator()
		{
            Console.WriteLine("UNFREEZING COORDINATOR");
            //ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "W", typeof(TrackerService));
			freezeC.Set();
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
                str += " "+work.Value.remaining+" \n";
            }
            str += "Current load: " + getLoad() + "\n";
            str += "=============\n";

            Console.WriteLine (str);
        }
	}
}

