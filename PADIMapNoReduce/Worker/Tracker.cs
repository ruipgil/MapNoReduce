using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

namespace PADIMapNoReduce
{
	public class Tracker : MarshalByRefObject, IWorkerService, IWorkingWorkerService
	{
		Dictionary<Guid, Job> currentJobs = new Dictionary<Guid, Job> ();
		/// <summary>
		/// To support the failure of a worker just before a worker was able to send the completed job message.
		/// In that case the workers may request directions to the new coordinator.
		/// </summary>
		List<Guid> completedJobs = new List<Guid>();
		List<string> knownWorkers = new List<string> ();
		Dictionary<string, IWorkingWorkerService> workersInstances = new Dictionary<string, IWorkingWorkerService>();

		public string ownAddress;

		public Tracker (string myAddress, int port)
		{
			this.ownAddress = myAddress+":"+port+"/W";

			TcpChannel channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(channel, false);
			RemotingServices.Marshal(this, "W", typeof(Tracker));

			knownWorkers.Add (ownAddress);
			workersInstances.Add (ownAddress, this);

			Console.WriteLine("I've been created with the address '"+ownAddress+"'");
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
			Utils.threadEachBlocking (toHeartbeat, (worker)=>{
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

		public void removeWorkers(List<string> workers) {
			knownWorkers = knownWorkers.Except (workers).ToList();
			Dictionary<Guid, Job> jobCoordinators = currentJobs.Where (x => workers.Contains (x.Value.coordinatorAddress)).ToDictionary(i=>i.Key, i=>i.Value);

			foreach (var job in jobCoordinators) {
				var aliveWorkers = job.Value.workers.Except (workers).ToList();
				if (aliveWorkers.First().Equals(ownAddress)) {
					takeOwnershipOfJob (job.Key);
				}
			}
		}

		/// <summary>
		/// Changes the job so that this tracker will be the coordinator.
		/// Updates the job in all workers.
		/// </summary>
		/// <param name="jobUuid">Job id.</param>
		public void takeOwnershipOfJob(Guid jobUuid) {
			Console.WriteLine ("I'm taking ownership of "+jobUuid);
			var job = currentJobs [jobUuid];

			prepareStartJob (job);
		}

		public void completedSplit(Guid job, int split) {
			Console.WriteLine ("Completed split "+job+"#"+split);
			currentJobs [job].splitCompleted (split);
		}

		public void completedJob(Guid job) {
			Console.WriteLine ("Completed job! " + job);
			completedJobs.Add (job);
			currentJobs.Remove (job);
		}

		public void newJob(Job job) {
			Console.WriteLine ("New job "+job);
			currentJobs.Add(job.Uuid, job);
		}

		public void updateJob(Job job) {
			Console.WriteLine ("Update job "+job);
			currentJobs [job.Uuid] = job;
		}

		public void submit(string clientAddress, int inputSize, int splits, byte[] code, string mapperName) {
			Console.WriteLine ("Submit "+clientAddress+", "+inputSize+", "+splits+", ..., "+mapperName);
			Job job = new Job (ownAddress, clientAddress, inputSize, splits, mapperName, code);
			currentJobs.Add (job.Uuid, job);

			prepareStartJob (job);
		}

		public void prepareStartJob(Job job) {
			// should we be careful with the workers in here?
			job.workers = knownWorkers.ToList ();

			// inform workers about this job
			Utils.threadEachBlocking(job.workers.Where(x=>!x.Equals (ownAddress)).ToList(), (worker)=>{
				getWorker(worker).newJob (job);
			});

			Utils.ExecInThread(() => startJob (job));
		}

		public void startJob(Job job) {
			try {
				Console.WriteLine ("Start job" + job);

				List<Split> splits = job.generateSplits ();
				Console.WriteLine ("Generated #"+splits.Count+" splits");
				int split = 0;
				while (job.nSplits > split) {
					Utils.threadEachBlocking(job.workers.ToList(), (worker)=>{
						Split s = splits[split++];
						Console.WriteLine("! Attributing "+s+" to "+worker);
						getWorker(worker).work (s);
					}, job.nSplits-split);
				}
				Console.WriteLine ("The job "+job+" as finished!");

				Utils.threadEachBlocking(job.workers.ToList(), (worker)=>{
					getWorker(worker).completedJob(job.Uuid);
				});
			}catch(Exception e) {
				Console.WriteLine (e);
			}
		}

		public void work(Split split) {
			Console.WriteLine ("Starting split "+split);
			var jobId = split.jobUuid;
			Job job = currentJobs[jobId];

			IMapper mapper = new SampleMapper ();

			// get and process at the same time TODO
			List<string> input = requestClient (job.clientAddress, split.lower, split.upper);
			foreach (var line in input) {
				/* result */
				mapper.Map (line);
			}

			Console.WriteLine ("%% Map phase of "+split+" ended");

			foreach (var worker in job.workers.ToList()) {
				getWorker(worker).completedSplit (jobId, split.id);
			}
		}

		public List<string> requestClient(string clientAddress, int lower, int upper) {
			Console.WriteLine ("Requested client "+clientAddress+" for values from "+lower+":"+upper);
			var client = (IClientService)Activator.GetObject (typeof(IClientService), clientAddress);
			return client.get (lower, upper);
		}

	}
}

