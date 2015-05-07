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
			knownWorkers = knownWorkers.Union (workers).ToList ();
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

		public void startHeartbeating() {
            //Disables communication if worker is freezed.
            if (freeze)
            {
                return;
            }
			
			List<string> toHeartbeat = currentJobs.Values.Where (elm=>{
				return !elm.Coordinator.Equals(ownAddress);
			}).Select(elm=>elm.Coordinator).ToList();//knownWorkers.ToList ();
			toHeartbeat.Remove (ownAddress);

			Console.WriteLine (" # Heartbeat ["+toHeartbeat.Count+"] "+string.Join (" ", toHeartbeat.ToArray()));
			if (toHeartbeat.Count < 1) {
				return;
			}

			var workersToDealWith = new List<string> ();
			Async.eachBlocking (toHeartbeat, (worker)=>{
				try {
					getWorker(worker).heartbeat(ownAddress);
				} catch(RemotingException) {
					Console.WriteLine ("# A worker is down: "+worker);
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
			//Console.WriteLine ("Known");
			//knownWorkers.ForEach (Console.WriteLine);

			var toTakeControl = currentJobs.Values.Where (job=>{
				return workers.Contains(job.Coordinator) &&
					job.Trackers.Except(knownWorkers).First().Equals(ownAddress);
			}).ToList();

			if (toTakeControl.Count < 1) {
				return;
			}

			Console.WriteLine ("to take control:");
			toTakeControl.ForEach (Console.WriteLine);
			workers.ForEach (Console.WriteLine);

			foreach (var job in toTakeControl) {
				takeOwnershipOfJob (job);
			}
		}

		/// <summary>
		/// Changes the job so that this tracker will be the coordinator.
		/// Updates the job in all workers.
		/// </summary>
		/// <param name="jobUuid">Job id.</param>
		public void takeOwnershipOfJob(Job job) {
			Console.WriteLine ("I'm taking ownership of "+job);

			var otherTrackers = job.Trackers;
			otherTrackers.Remove (ownAddress);
			job.Coordinator = ownAddress;
			job.Trackers = getReliableTrackers();

			Async.eachBlocking (job.Trackers.Union(otherTrackers).ToList(), (tracker) => {
				Console.WriteLine ("Saying "+tracker+" to update "+job);
				getWorker (tracker).announceJob (job);
			});

			Async.ExecInThread(() => startJob (job));
		}

		public void completedSplit(Guid job, int split) {
			Console.WriteLine ("Completed split "+job+"#"+split);
			if (currentJobs.ContainsKey (job)) {
				currentJobs [job].splitCompleted (split);
			}
		}

		public void completedJob(Guid job) {
			Console.WriteLine ("Completed job! " + job);
			currentJobs.Remove (job);
		}

		public void announceJob(Job job) {
			Console.WriteLine ("Announced job "+job);
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
			var list = knownWorkers.Where (w => w != ownAddress).ToList ();
			return list.GetRange (0, list.Count > 0 ? 1 : 0);
		}

		public void submit(string clientAddress, int inputSize, long fileSize,  int splits, byte[] code, string mapperName) {
			//receives a submit, therefore the job tracker
            jt = true;
            
            Console.WriteLine ("Submit "+clientAddress+", "+inputSize+", "+splits+", ..., "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			job.Trackers = getReliableTrackers();

			if (job.hasReplicas()) {
				Async.eachBlocking (job.Trackers, (tracker) => {
					Console.WriteLine ("Saying "+tracker+" about "+job);
					getWorker (tracker).announceJob (job);
				});
			}

			Async.ExecInThread(() => startJob (job));
		}

		public List<string> getWorkersByLoad() {
			var list = knownWorkers.ToList ();
			list.Add (ownAddress);
			return list;
		}

		public void startJob(Job job) {
            
            Console.WriteLine (job.debugDump ());

			Console.WriteLine ("Start job" + job);

			List<Split> splits = job.generateSplits ();
			Console.WriteLine ("Generated #"+splits.Count+" splits");

			int split = 0;
			int toComplete = splits.Count;
			while (splits.Count > split) {
				var wList = getWorkersByLoad();
				Async.eachBlocking(wList, (worker)=>{
					Split s = splits[split++];
					Console.WriteLine("! Attributing "+s+" to "+worker);
					try {
                            getWorker(worker).work (s);
						    // if the worker returns the worker doesn't need to receive completedSplit,
						    // sparing network traffic.
						    completedSplit(job.Uuid, s.id);
					} catch(RemotingException e) {
						// TODO remove worker?
						Console.WriteLine("Remote error!");
						Console.WriteLine(e);
					} catch( Exception e) {
						Console.WriteLine("...");
						Console.WriteLine(e);
					}
				}, splits.Count-split);
			}
			Console.WriteLine ("The job "+job+" as finished!");

			if( job.hasReplicas() ) {
				// Doesn't need to be blocking?
				Async.eachBlocking (job.Trackers, (tracker) => {
					Console.WriteLine ("Saying "+tracker+" that "+job+" has finished!");
					getWorker (tracker).completedJob (job.Uuid);
				});
			}

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
            while (freeze && !jt);


            Console.WriteLine ("Starting split "+split);
			Job job = split.Job;

			instanceLoad.Add (split.ToString(), split.upper-split.lower);

			IMapper mapper = new SampleMapper ();

			var results = new List<IList<KeyValuePair<string, string>>> ();

			// get and process at the same time TODO
			requestClient (job.Client, split, (subSplitResult)=>{
				foreach(var line in subSplitResult) {
					results.Add(mapper.Map (line));
				}
			}, ()=>{
				instanceLoad.Remove (split.ToString());

				Console.WriteLine ("%% Map phase of "+split+" ended");

				Async.each (job.Trackers, (tracker) => {
					Console.WriteLine ("Saying "+tracker+" that "+split+" has finished!");
					getWorker (tracker).completedSplit (job.Uuid, split.id);
				});
			});
		}

		public void requestClient(string clientAddress, Split s, Action<List<string>> exec, Action onEnd) {
			Console.WriteLine (s.lower+"---"+s.upper);
			var totalLines = s.Job.InputSize;
			var size = s.Job.InputSizeBytes;
			var lines = s.upper - s.lower;

			var averagePerLine = size / (float)totalLines;
			var predictedSize = lines * averagePerLine;

			int MAX_TRANSFER_MB = 1;
			long MAX_TRANSFERSIZE = MAX_TRANSFER_MB * 1000 * 1000;

			var ns = Math.Ceiling(predictedSize/(double)MAX_TRANSFERSIZE);
			var t = lines / (int)ns;
			var last = s.lower;
			var chunks = new List<Tuple<int, int>> ();
			//Console.WriteLine ("File size: "+size+"\nLines: "+totalLines+" MAX: "+MAX_TRANSFERSIZE+"\nAverageSize: "+averagePerLine+"\nPredictedSize: "+predictedSize+"\nns: "+ns+"\nt: "+t);
			for (var i = 0; i < ns; i++) {
				var m = last + t;
				chunks.Add(new Tuple<int, int>(last, m));
				//Console.WriteLine (last+"-"+m);
				last = m;
			}
			if (last != s.upper) {
				chunks.Add (new Tuple<int, int>(s.lower, s.upper));
			}

			var N_PARALLEL = 1;
			var executed = 0;
			Async.eachLimitBlocking (chunks, (chunk)=>{
				var result = requestClient (clientAddress, chunk.Item1, chunk.Item2);
				Async.ExecInThread(()=>{
					exec (result);
					executed ++;
					if(executed>=chunks.Count) {
						onEnd();
					}
				});
			}, N_PARALLEL);
		}

		public List<string> requestClient(string clientAddress, int lower, int upper) {
			Console.WriteLine ("Requested client "+clientAddress+" for values from "+lower+":"+upper);
			var client = (IClientService)Activator.GetObject (typeof(IClientService), clientAddress);
			Console.WriteLine (lower+" "+upper);
			return client.get (lower, upper);
		}

		public int GetLoad () {
			return instanceLoad.Values.Sum ();
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

