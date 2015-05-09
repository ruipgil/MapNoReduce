using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    /// <summary>
    /// Worker interface visible to other workers
    /// </summary>
    public interface IWorkingWorkerService : IWorkerService
    {
		void work(Split split);

		/// <summary>
		/// Heartbeats a worker, with the address of another worker.
		/// If the worker is unknown it will be added to the known worker list of this instance.
		/// If the worker is known, then it works like a heartbeat should. The lease time should be renewed.
		/// 
		/// The address could be removed if we could get the IP of the resquest.
		/// </summary>
		/// <param name="workerAddress">Worker's address.</param>
		void heartbeat(string workerAddress);

		void completedSplit(Guid job, int splitId);
		void completedJob(Guid job);
		void announceJob(Job job);

		void getStatus ();

        bool isFreezed();

		void assignSplit(Guid job, int split, string worker);
		void deassignSplit(Guid job, int split);
		int getSplitStatus(Guid job, int split);
		void shareKnownWorkers (string sender, HashSet<string> workers);
		int getLoad();
    }
}