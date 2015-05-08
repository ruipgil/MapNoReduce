using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading;

namespace PADIMapNoReduce
{
	public class Tracker : MarshalByRefObject, IWorkerService, IWorkingWorkerService
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
		List<string> knownWorkers = new List<string> ();
		Dictionary<string, IWorkingWorkerService> workersInstances = new Dictionary<string, IWorkingWorkerService>();
		Dictionary<string, int> instanceLoad = new Dictionary<string, int> ();
		Dictionary<string, Tuple<decimal, int>> knownWorkersLoad = new Dictionary<string, Tuple<decimal, int>> ();
		//Dictionary<string, List<Thread>> executingSplits = new Dictionary<string, List<Thread>>();

		const int MAX_TRANSFER_MB = 1;
		const int N_PARALLEL = 1;

		const int LOAD = 10000;

		public string ownAddress;
        
        //milliseconds
        private int slow = 0;
        private bool freeze = false;
        private bool jt = false;

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

		public void addKnownWorkers(List<string> workers) {
			workers.Remove (ownAddress);
			knownWorkers = knownWorkers.Union (workers).ToList ();

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
			if (workersInstances.ContainsKey (address)) {
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

		public void updateKnownWorkersStatus() {
			/*Async.eachBlocking(knownWorkers.ToList(), worker=>{
				try {
					var status = getWorker(worker).getStatus(ownAddress);

					addKnownWorkers (knownWorkers);

					var info = new Tuple<decimal, int> (status.load, status.tracking);
					if (knownWorkersLoad.ContainsKey (worker)) {
						knownWorkersLoad [worker] = info;
					} else {
						knownWorkersLoad.Add (worker, info);
					}
				} catch(RemotingException) {
					removeWorkers(worker);
				}
			});*/
		}

		public StatusInfo getStatus(string requester) {
			addKnownWorkers (new List<string>(){ requester });
			return new StatusInfo (ownAddress, knownWorkers, GetLoad(), currentJobs.Values.Count);
		}

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

		/// <summary>
		/// Removes workers from the known worker list.
		/// It will check if any of these workers are coordinator. If any of them is, it will check
		/// if this instance is next in line to take over the coordination of a job.
		/// </summary>
		/// <param name="workers">Workers.</param>
		public void removeWorkers(List<string> workers) {
			knownWorkers = knownWorkers.Except (workers).ToList();
			workers.ForEach (w=>knownWorkersLoad.Remove(w));

			var toTakeControl = currentJobs.Values.Where (job=>{
				return workers.Contains(job.Coordinator) &&
					job.Trackers.Except(knownWorkers).First().Equals(ownAddress);
			}).ToList();

			if (toTakeControl.Count < 1) {
				return;
			}
			foreach (var job in toTakeControl) {
				takeOwnershipOfJob (job);
			}

			var toCreateReplica = currentJobs.Values.Where (job=>{
				return job.Trackers.Except(workers).Count() == 0;
			}).ToList();

			foreach (var job in toCreateReplica) {
				assignReplica (job);
			}
		}

		public void removeWorkers(string worker) {
			removeWorkers (new List<string> (){ worker });
		}

		public void assignReplica(Job job) {
			job.Trackers = getReliableTrackers ();
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

			assignReplica (job);

			Async.ExecInThread(() => startJob (job));
		}

		public void completedSplit(Guid job, int split) {
			Console.WriteLine ("[Split]C "+job+"#"+split);
			if (currentJobs.ContainsKey (job)) {
				currentJobs [job].splitCompleted (split);
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

		public List<string> getReliableTrackers() {
			/*updateKnownWorkersStatus ();*/
			var list = knownWorkers.Where (w => w != ownAddress).ToList ();
			return list.GetRange (0, list.Count > 0 ? 1 : 0);
		}

		public void submit(string clientAddress, int inputSize, long fileSize,  int splits, byte[] code, string mapperName) {
			//receives a submit, therefore the job tracker
            jt = true;
            
			Console.WriteLine ("[Submit]\n\tClient: "+clientAddress+"\n\tLines: "+inputSize+"\n\tSize: "+fileSize+" Bytes\n\tMapper Name: "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			assignReplica (job);

			Async.ExecInThread(() => startJob (job));
		}

		public List<string> getWorkersByLoad() {
			/*updateKnownWorkersStatus ();
			var d = knownWorkersLoad.OrderBy (x => x.Value.Item1).ToDictionary(x=>x.Key, x=>x.Value);
			foreach (var a in d) {
				Console.WriteLine (a.Key+" - ("+a.Value.Item1+" ~ "+a.Value.Item2+")");
			}

			return d.Keys.ToList();*/
            var w = knownWorkers.ToList();
			w.Add (ownAddress);
            return w;
		}

		public void startJob(Job job) {
			Console.WriteLine ("[Job]> "+job);

			Queue<Split> splits = new Queue<Split>(job.generateSplits ());
			Console.WriteLine ("[Job]I "+job+" gens "+splits.Count+" splits");

			while (splits.Count>0) {
				var wList = getWorkersByLoad();
				Async.eachBlocking(wList, (worker)=>{
					Split s = splits.Dequeue();
					Console.WriteLine ("[Job]I "+s+" to "+worker);
					try {
						getWorker(worker).work (s);
						// if the worker returns the worker doesn't need to receive completedSplit,
						// sparing network traffic.
						completedSplit(job.Uuid, s.id);
					} catch(RemotingException) {
						splits.Enqueue(s);
						removeWorkers(worker);
					} catch(Exception e) {

						Console.WriteLine(e);
					}
				}, splits.Count);
			}

			if( job.hasReplicas() ) {
				Async.eachBlocking (job.Trackers, (tracker) => {
					Console.WriteLine ("[Job]I "+job+" to "+tracker);
					try {
						getWorker (tracker).completedJob (job.Uuid);
					} catch(RemotingException) {
						removeWorkers(tracker);
					}
				});
			}

			var client = (IClientService)Activator.GetObject (typeof(IClientService), job.Client);
			client.done ();

			Console.WriteLine ("[Job]< "+job);

			currentJobs.Remove (job.Uuid);
		}

		public void work(Split split) {
            
            //simulates worker slowing down 
            if (slow != 0)
            {
                Console.WriteLine("Falling asleep {0} milliseconds ....", slow);
                Thread.Sleep(slow);
            }

            //Simulates worker freezing
			// TODO: Should be a lock?
            while (freeze && !jt);

			if (instanceLoad.ContainsKey (split.ToString ())) {
				// TODO instanceLoad[split.ToString()].Thread.Join();
				return;
			}


            Console.WriteLine ("[Split]> "+split);
			Job job = split.Job;

			//Thread.Sleep(3000);

			instanceLoad.Add (split.ToString(), split.upper-split.lower);

			IMapper mapper = new SampleMapper ();

			var results = new List<IList<KeyValuePair<string, string>>> ();

			var client = (IClientService)Activator.GetObject (typeof(IClientService), job.Client);

			var lines = client.get(split.lower, split.upper);
			//Console.WriteLine (lines.Count+" expected "+(split.upper-split.lower));
			foreach(var line in lines) {
				results.Add(mapper.Map (line));
			}

			// data replication:
			//  - start sending client and at the same time send copy to trackers
			//  - when ends sending to client signals trackes to delete info.
			client.set(split.id, results);

			instanceLoad.Remove (split.ToString());

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

		public int GetLoad () {
			return instanceLoad.Values.Sum () / LOAD;
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

        public bool isJobTracker()
        {
            return jt;
        }

        public bool isFreezed()
        {
            return freeze;
        }
        
        public void getStatus()
        {
            String properties = " Job Tracker: " + jt + " Failing: "+freeze+"\n";
            String kw = " Known Workers: ";
            foreach (String w in knownWorkers)
            {
                kw += w + " ";
            }
            kw += "\n";
            String jobs = " On going jobs: ";
            foreach(KeyValuePair<Guid,Job> j in currentJobs)
            {
                jobs += j.Key + " ";
            }
            jobs += "\n";
            Console.WriteLine("STATUS:\n" + properties + kw + jobs );
        }
	}
}

