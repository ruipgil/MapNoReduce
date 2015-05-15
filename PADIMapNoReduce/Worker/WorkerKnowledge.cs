using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace PADIMapNoReduce
{
	public abstract class WorkerKnowledge : MarshalByRefObject
	{
		private string ownAddress;
		HashSet<string> knownWorkers = new HashSet<string> ();
		Dictionary<string, IWService> workersInstances = new Dictionary<string, IWService>();

		ManualResetEvent freezeC = new ManualResetEvent(true);

		public string Address { get { return ownAddress; } }
		public HashSet<string> KnownWorkers { get { return knownWorkers; } }

		public WorkerKnowledge (int port) : this("tcp://localhost", port) {}
		public WorkerKnowledge (string address, int port) {
			ownAddress = address + ":" + port + "/W";
			Console.WriteLine ("Worker created @ "+ownAddress);
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}

		public virtual void addKnownWorkers(List<string> workers) {
			workers.Remove (Address);
			workers.ForEach(w => knownWorkers.Add(w));
		}

		/// <summary>
		/// Gets a worker instance based on its address.
		/// </summary>
		/// <returns>The worker.</returns>
		/// <param name="address">Address.</param>
		public IWService getWorker(string address) {
			freezeC.WaitOne();

			if(workersInstances.ContainsKey (address)) {
				if(address.Equals(Address)) {
					// TODO? Dirty trick
					return (IWService)this;
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

		public void shareKnownWorkers(string sender, HashSet<string> workers) {
			freezeC.WaitOne();

			workers.Remove(ownAddress);
			workers.Add(sender);

			addKnownWorkers(workers.ToList());
		}

		public void startSharingKnownWorkers() {
			Async.ExecInThread (() => {
				while (true) {
					freezeC.WaitOne ();

					Async.eachBlocking (knownWorkers.ToList (), (worker) => {
						try {
							getWorker (worker).shareKnownWorkers (ownAddress, knownWorkers);
						} catch (Exception) {
						}
					});

					Thread.Sleep (Const.SHARE_WORKERS_INTERVAL_MS);
				}
			});
		}

		/// <summary>
		/// Removes workers from the known worker list.
		/// It will check if any of these workers are coordinator. If any of them is, it will check
		/// if this instance is next in line to take over the coordination of a job.
		/// </summary>
		/// <param name="workers">Workers.</param>
		public virtual void removeWorkers(List<string> workers) {

			workers.Remove (ownAddress);
			workers.ForEach (w=>knownWorkers.Remove(w));
		}

		public void removeWorkers(string worker) {
			removeWorkers (new List<string> (){ worker });
		}
	}
}

