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
			
			List<string> toHeartbeat = knownWorkers.ToList ();
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

		public void submit(string clientAddress, int inputSize, int splits, byte[] code, string mapperName) {
			Console.WriteLine ("Submit "+clientAddress+", "+inputSize+", "+splits+", ..., "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, splits, mapperName, code);
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
			Console.WriteLine ("Starting split "+split);
			Job job = split.Job;

			instanceLoad.Add (split.ToString(), split.upper-split.lower);

			IMapper mapper = new ParadiseCountMapper ();

			// get and process at the same time TODO
			List<string> input = requestClient (job.Client, split.lower, split.upper);
			var results = new List<IList<KeyValuePair<string, string>>> ();
			foreach (var line in input) {
				/* result */
				results.Add(mapper.Map (line));
			}

			instanceLoad.Remove (split.ToString());

			Console.WriteLine ("%% Map phase of "+split+" ended");

			Async.each (job.Trackers, (tracker) => {
				Console.WriteLine ("Saying "+tracker+" that "+split+" has finished!");
				getWorker (tracker).completedSplit (job.Uuid, split.id);
			});
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

