using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;

namespace PADIMapNoReduce
{
	public class Tracker : Worker, IWorkerService, IWService, IRemoteTesting
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
		HashSet<Guid> jobsCompleted = new HashSet<Guid>();
		ManualResetEvent freezeC = new ManualResetEvent(true);
		const int TRACKER_OVERHEAD_VS_WORKER = 100;

		public Tracker (int port) : base(port) {}
		public Tracker (string address, int port) : base(address, port) {
			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			RemotingServices.Marshal(this, "W", typeof(Tracker));
		}

		public void heartbeat(string workerAddress) {
			freezeC.WaitOne();

			//Console.WriteLine (" # Received heartbeat of "+workerAddress);
			if (!KnownWorkers.Contains (workerAddress)) {
				KnownWorkers.Add (workerAddress);
			}
		}

		public void startHeartbeating() {
			freezeC.WaitOne();

			List<string> toHeartbeat = new List<string> ();
			foreach (var job in currentJobs.Values) {
				toHeartbeat.Add (job.Coordinator);
				toHeartbeat.AddRange (job.Trackers);
			}
			toHeartbeat = toHeartbeat.Distinct ().ToList ();
			toHeartbeat.Remove (Address);

			//Console.WriteLine (" # Heartbeat ["+toHeartbeat.Count+"] "+string.Join (" ", toHeartbeat.ToArray()));
			if (toHeartbeat.Count < 1) {
				return;
			}

			var workersToDealWith = new List<string> ();
			Async.eachBlocking (toHeartbeat, (worker)=>{
				try {
					//Console.WriteLine("Heartbeating: " + worker);
					getWorker(worker).heartbeat(Address);
					//Console.WriteLine("Success: " + worker);
				} catch(Exception) {
					//Console.WriteLine ("# A worker is down: "+worker);
					workersToDealWith.Add (worker);
				}
			});
			// different process
			removeWorkers (workersToDealWith);
		}

		public void assignReplica(Job job) {
			Console.WriteLine ("assigning replica");

			var workers = getWorkersByLoad ();
			workers.Remove (Address);
			job.Trackers = workers.GetRange(0, Math.Min(1, workers.Count));
			Console.WriteLine (": "+job.Trackers.First());
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
			Console.WriteLine ("finish replicating");
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
			otherTrackers.Remove (Address);
			job.Coordinator = Address;

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
			if (job.Trackers.Contains (Address)) {
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
			Job job = new Job (Address, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			Console.WriteLine ("assigning replica");
			assignReplica (job);

			Console.WriteLine ("starting job");

			Async.ExecInThread(() => startJob (job));
		}

		public List<string> getWorkersByLoad() {
			freezeC.WaitOne();
			var workers = new List<Tuple<string, int>> ();
			Async.eachBlocking (KnownWorkers.ToList (), (worker) => {
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
			workers.Add (new Tuple<string, int>(Address, getLoad()));
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
							//Console.WriteLine("-");
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

		public override int getLoad () {
			return currentJobs.Values.Count * TRACKER_OVERHEAD_VS_WORKER + base.getLoad ();
		}

		public override void addKnownWorkers(List<string> workers) {
			base.addKnownWorkers (workers);

			currentJobs.Values.Where (job=>{
				return job.Trackers.Count == 0;
			}).ToList().ForEach(assignReplica);
		}

		public override void removeWorkers(List<string> workers) {
			base.removeWorkers (workers);

			var toTakeControl = currentJobs.Values.Where (job=>{
				return workers.Contains(job.Coordinator) &&
					job.Trackers.Except(KnownWorkers).First().Equals(Address);
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

		public void freezeCoordinator()
		{
			Console.WriteLine("FREEZING COORDINATOR");
			freezeC.Reset();
			// TOFIX
			RemotingServices.Marshal(this, "W", typeof(IRemoteTesting));
		}

		public void unfreezeCoordinator()
		{
			Console.WriteLine("UNFREEZING COORDINATOR");
			// TOFIX
			RemotingServices.Marshal(this, "W", typeof(Tracker));
			freezeC.Set();
		}

		public override void getStatus()
		{
			int nTracking = currentJobs.Values.Where(x=>x.Coordinator.Equals(Address)).Count();
			int nReplicas = currentJobs.Values.Where(x => x.Trackers.Contains(Address)).Count();
			string str = "=============\n";
			str+="This worker is the coordinator of "+nTracking+" jobs and is the replica of "+nReplicas+".\n";
			str += "Job list:\n";
			foreach (var job in currentJobs.Values)
			{
				str += job.debugDump();
			}
			str += "This worker known about: " + string.Join(", ", KnownWorkers.ToArray())+"\n";

			str += "Current works:\n";
			Console.WriteLine (str);

			base.getStatus ();

			str = "Current load: " + getLoad() + "\n";
			str += "=============\n";
			Console.WriteLine (str);
		}
	}
}

