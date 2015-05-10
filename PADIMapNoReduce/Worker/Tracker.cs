﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;
using System.Collections.Concurrent;

namespace PADIMapNoReduce
{
	public struct WorkInfo {
		public Split split;
		public Thread thread;
		public int remaining;
        public WorkStatus status;
		public DateTime started;
	}

	public class Tracker : MarshalByRefObject, IWorkerService, IWorkingWorkerService
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
		HashSet<string> knownWorkers = new HashSet<string> ();
		Dictionary<string, IWorkingWorkerService> workersInstances = new Dictionary<string, IWorkingWorkerService>();
		Dictionary<string, WorkInfo> instanceLoad = new Dictionary<string, WorkInfo> ();
		Dictionary<string, Tuple<decimal, int>> knownWorkersLoad = new Dictionary<string, Tuple<decimal, int>> ();
		HashSet<string> splitsDone = new HashSet<string>();
		//Dictionary<string, List<Thread>> executingSplits = new Dictionary<string, List<Thread>>();

		const int MAX_TRANSFER_MB = 1;
		const int N_PARALLEL = 1;
		const int N_PARALLEL_MAP_PER_JOB = 8;
		const int LOAD = 10000;

		public string ownAddress;
        
        //milliseconds
        private int slow = 0;
        private bool freeze = false;

		public Tracker (string myAddress, int port)
		{
			this.ownAddress = myAddress+":"+port+"/W";

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			RemotingServices.Marshal(this, "W", typeof(Tracker));

			//knownWorkers.Add (ownAddress);
			workersInstances.Add (ownAddress, this);

			Console.WriteLine("Worker created at '"+ownAddress+"'");
		}

		public Tracker(int port) : this("tcp://localhost", port) {
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
		public IWorkingWorkerService getWorker(string address) {
			//Console.WriteLine ("Getting worker "+address);
			if(workersInstances.ContainsKey (address)) {
				if(address.Equals(ownAddress)) {
					return this;
				}
				return workersInstances [address];
			} else {
				// instantiate worker
				IWorkingWorkerService worker = (IWorkingWorkerService)Activator.GetObject(typeof(IWorkingWorkerService), address);
				workersInstances.Add(address, worker);
				knownWorkers.Add (address);
				return worker;
			}
		}

		// this can give more info
		// - list of the workers he knows
		// - it's load
		public void heartbeat(string workerAddress) {
			//Console.WriteLine (" # Received heartbeat of "+workerAddress);
			if (!knownWorkers.Contains (workerAddress)) {
				knownWorkers.Add (workerAddress);
			}
		}

		public void shareKnownWorkers(string sender, HashSet<string> workers) {
			workers.Remove (ownAddress);
			workers.Add (sender);
			foreach(var worker in workers) {
				knownWorkers.Add (worker);
			}
		}

		/*public StatusInfo getStatus(string requester) {
			addKnownWorkers (new List<string>(){ requester });
			return new StatusInfo (ownAddress, knownWorkers, GetLoad(), currentJobs.Values.Count);
		}*/

		public void startHeartbeating() {
            //Disables communication if worker is freezed.
            if (freeze)
            {
                return;
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
				} catch(RemotingException) {
					//Console.WriteLine ("# A worker is down: "+worker);
					workersToDealWith.Add (worker);
				} catch(Exception e) {
					Console.WriteLine ("Error!");
					Console.WriteLine (e);
				}
			});
			// different process
			removeWorkers (workersToDealWith);
		}

		public void startSharingKnownWorkers() {
			Async.eachBlocking (knownWorkers.ToList(), (worker)=> {
				try {
					getWorker(worker).shareKnownWorkers(ownAddress, knownWorkers);
				} catch(RemotingException) {}
			});
		}

		/// <summary>
		/// Removes workers from the known worker list.
		/// It will check if any of these workers are coordinator. If any of them is, it will check
		/// if this instance is next in line to take over the coordination of a job.
		/// </summary>
		/// <param name="workers">Workers.</param>
		public void removeWorkers(List<string> workers) {
			//Console.WriteLine ("I'll remove :"+string.Join(",", workers));
			//Console.WriteLine ("Knownn workers: "+string.Join(",", knownWorkers));

			workers.Remove (ownAddress);
			workers.ForEach (w=>knownWorkers.Remove(w));
			workers.ForEach (w=>knownWorkersLoad.Remove(w));

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
				//Console.WriteLine ("assign replica");
				assignReplica (job);
			}

			foreach (var job in currentJobs.Values) {
				workers.ForEach (job.removeAssignmentsFromWorker);
			}

			//Console.WriteLine ("2 Knownn workers: "+string.Join(",", knownWorkers));
		}

		public void removeWorkers(string worker) {
			removeWorkers (new List<string> (){ worker });
		}

		public void assignReplica(Job job) {
			var workers = getWorkersByLoad ();
			workers.Remove (ownAddress);
			job.Trackers = workers;
			if (job.Trackers.Count > 0) {
				Async.eachBlocking (job.Trackers.ToList (), (tracker) => {
					Console.WriteLine ("[Job]Update " + job + " to " + tracker);
					try {
						getWorker (tracker).announceJob (job);
					} catch(RemotingException) {
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
					//Console.WriteLine("STATUS: "+assignment.Value+" "+assignment.Key+" . "+status);
				} catch(RemotingException) {
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
			var splitId = job + "#" + split;
			Console.WriteLine ("[Split]C "+splitId);
			if (currentJobs.ContainsKey (job)) {
				currentJobs [job].splitCompleted (split);
				currentJobs [job].deassign (split);
			}
		}

		public void completedJob(Guid job) {
			Console.WriteLine ("[Job]C " + job);
			currentJobs.Remove (job);
		}

		public void announceJob(Job job) {
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
				} catch(RemotingException) {
					removeWorkers(worker);
				}
			});
			workers.Add (new Tuple<string, int>(ownAddress,getLoad()));

			//var temp = workers.OrderByDescending (x => x.Item2);
			/*Console.WriteLine ("Ordered workers: ");
			foreach (var t in temp) {
				Console.Write (t.Item1+" "+t.Item2);
			}
			Console.WriteLine ();*/

			return workers.OrderByDescending (x => x.Item2).Select(x=>x.Item1).ToList();
		}

		public void assignSplit(Guid jobId, int splitId, string worker) {
			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.assign (splitId, worker);
			}
		}

		public void deassignSplit(Guid jobId, int split) {
			if (currentJobs.ContainsKey (jobId)) {
				var job = currentJobs [jobId];
				job.deassign (split);
			}
		}

		public void informReplicas(Job job, Action<string> action) {
			if( job.hasReplicas() ) {
				Async.eachBlocking (job.Trackers, (w)=>{
					try {
						action(w);
					 } catch (RemotingException) {
						removeWorkers (w);
					}
				});
			}
		}

		public void startJob(Job job) {
			Console.WriteLine ("[Job]> "+job);

			Queue<Split> splits = new Queue<Split>(job.generateSplits ());
			Console.WriteLine ("[Job]I "+job+" gens "+splits.Count+" splits");

			do {
				splits = new Queue<Split> (job.generateSplits ());

				Console.WriteLine (job.debugDump ());
				var wList = getWorkersByLoad ();
				Async.eachBlocking (wList, (worker) => {
					Split s = splits.Dequeue ();
					Console.WriteLine ("[Job]I " + s + " to " + worker);
					try {
						job.assign (s.id, worker);
						var w = getWorker (worker);
						informReplicas (job, (tracker) => {
							getWorker (tracker).assignSplit (job.Uuid, s.id, worker);
						});
						w.work (s);

						completedSplit (job.Uuid, s.id);
					} catch (RemotingException) {
						splits.Enqueue (s);
						removeWorkers (worker);

						informReplicas (job, (tracker) => {
							getWorker (tracker).deassignSplit (job.Uuid, s.id);
						});
					} catch (Exception e) {

						Console.WriteLine (e);
					}
				}, splits.Count);
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

		public void work(Split split) {
			if (ownAddress.EndsWith ("30003/W")) {
				Thread.Sleep (30000);
			}
            //simulates worker slowing down 
            if (slow != 0)
            {
                Console.WriteLine("[Split]Slow {0} milliseconds ....", slow);
                Thread.Sleep(slow);
            }

            //Simulates worker freezing
			// TODO: Should be a lock?
            while (freeze);

			if (instanceLoad.ContainsKey (split.ToString ())) {
				Console.WriteLine ("[Split]Join "+split);
				instanceLoad[split.ToString()].thread.Join();
				return;
			}


            Console.WriteLine ("[Split]> "+split);
			Job job = split.Job;

			WorkInfo winfo = new WorkInfo();
			winfo.started = DateTime.Now;
			winfo.split = split;
			winfo.remaining = split.upper - split.lower;
			winfo.thread = Thread.CurrentThread;
            winfo.status = WorkStatus.Getting;
			instanceLoad.Add (split.ToString(), winfo);

			IMapper mapper = new SampleMapper ();

			var results = new ConcurrentQueue<IList<KeyValuePair<string, string>>> ();

			var client = (IClientService)Activator.GetObject (typeof(IClientService), job.Client);
			var lines = client.get(split.lower, split.upper);

			winfo.status = WorkStatus.Mapping;
            
			//Console.WriteLine (lines.Count+" expected "+(split.upper-split.lower));
            // TODO async.limiting
			//Parallel.ForEach(lines, 
			Async.eachLimitBlocking(lines, (line)=>{
				results.Enqueue(mapper.Map (line));
				winfo.remaining--;
			}, Math.Min(N_PARALLEL_MAP_PER_JOB, winfo.remaining));
			//Console.WriteLine ("%");

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

			Console.WriteLine ("[Split]<  "+split);

			Async.each (job.Trackers, (tracker) => {
				Console.WriteLine ("[Split]I  "+split+" to "+tracker);
				try {
					getWorker (tracker).completedSplit (job.Uuid, split.id);
				} catch(RemotingException) {
					removeWorkers(tracker);
				}
			});
		}

		public const int TRACKER_OVERHEAD_VS_WORKER = 100;

		public int getLoad () {
			if (freeze) {
				return Int32.MaxValue;
			}else{
                var s = (slow == 0 ? 1 : (slow / 1000));
				return s * currentJobs.Values.Count * TRACKER_OVERHEAD_VS_WORKER + instanceLoad.Values.Sum(x=>x.remaining) * s / N_PARALLEL_MAP_PER_JOB;
			}
		}

        public void slowWorker(int seconds)
        {
            Console.WriteLine("Worker is slowing down {0} seconds ...", seconds);
            slow = seconds * 1000;

        }

        public void freezeWorker()
        {
            freeze = true;
        }

        public void unFreezeWorker()
        {
            freeze = false;
        }

        public bool isFreezed()
        {
            return freeze;
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

