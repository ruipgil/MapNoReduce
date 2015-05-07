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
		//Dictionary<string, List<Thread>> executingSplits = new Dictionary<string, List<Thread>>();

		int MAX_TRANSFER_MB = 1;
		int N_PARALLEL = 1;

		public string ownAddress;

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
				return job.Trackers.Except(knownWorkers).Count() == 0;
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
					Console.WriteLine ("Saying " + tracker + " to update " + job);
					getWorker (tracker).announceJob (job);
				});
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

			assignReplica (job);

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
			Console.WriteLine ("Submit "+clientAddress+", "+inputSize+", "+splits+", ..., "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, fileSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			assignReplica (job);

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

			Queue<Split> splits = new Queue<Split>(job.generateSplits ());
			Console.WriteLine ("Generated #"+splits.Count+" splits");

			while (splits.Count>0) {
				var wList = getWorkersByLoad();
				Async.eachBlocking(wList, (worker)=>{
					Split s = splits.Dequeue();
					Console.WriteLine("! Attributing "+s+" to "+worker);
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
			Console.WriteLine ("The job "+job+" as finished!");

			if( job.hasReplicas() ) {
				Async.eachBlocking (job.Trackers, (tracker) => {
					Console.WriteLine ("Saying "+tracker+" that "+job+" has finished!");
					try {
						getWorker (tracker).completedJob (job.Uuid);
					} catch(RemotingException) {
						removeWorkers(tracker);
					}
				});
			}

			// TODO: signal client

			currentJobs.Remove (job.Uuid);
		}

		public void work(Split split) {
			Console.WriteLine ("Starting split "+split);
			Job job = split.Job;

			Thread.Sleep(5000);

			instanceLoad.Add (split.ToString(), split.upper-split.lower);

			IMapper mapper = new ParadiseCountMapper ();

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
					try {
						getWorker (tracker).completedSplit (job.Uuid, split.id);
					} catch(RemotingException) {
						removeWorkers(new List<string>(){ tracker });
					}
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

			long MAX_TRANSFERSIZE = MAX_TRANSFER_MB * 1000 * 1000;

			var ns = Math.Ceiling(predictedSize/(double)MAX_TRANSFERSIZE);
			var t = lines / (int)ns;
			var last = s.lower;
			var chunks = new List<Tuple<int, int>> ();
			for (var i = 0; i < ns; i++) {
				var m = last + t;
				chunks.Add(new Tuple<int, int>(last, m));
				last = m;
			}
			if (last != s.upper) {
				chunks.Add (new Tuple<int, int>(s.lower, s.upper));
			}

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
	}
}

