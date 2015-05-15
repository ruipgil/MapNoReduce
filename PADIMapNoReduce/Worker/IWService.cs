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
    public interface IWService
    {
		/// <summary>
		/// Work the specified split.
		/// </summary>
		/// <param name="split">Split.</param>
		void work(Split split);

		/// <summary>
		/// Heartbeats a worker, with the address of another worker.
		/// If the worker is unknown it will be added to the known worker list of this instance.
		/// If the worker is known, then it works like a heartbeat should.
		/// 
		/// The address could be removed if we could get the IP of the resquest.
		/// </summary>
		/// <param name="workerAddress">Worker's address.</param>
		void heartbeat(string workerAddress);

		/// <summary>
		/// Informs the tracker that a split has finish.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="splitId">Split identifier.</param>
		void completedSplit(Guid job, int splitId);

		/// <summary>
		/// Informs the tracker that the job has been completed.
		/// </summary>
		/// <param name="job">Job.</param>
		void completedJob(Guid job);

        //void completedJobs(HashSet<Guid> job);

		/// <summary>
		/// Announces the job to a tracker.
		/// The receiver should either add it or, in case it already as, uppdate it.
		/// </summary>
		/// <param name="job">Job.</param>
		void announceJob(Job job);

		/// <summary>
		/// Assigns the split to a worker.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="split">Split.</param>
		/// <param name="worker">Worker.</param>
		void assignSplit(Guid job, int split, string worker);

		/// <summary>
		/// Deassigns the split, that was assigned to a worker.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="split">Split.</param>
		void deassignSplit(Guid job, int split);

		/// <summary>
		/// Gets the status of a split.
		/// </summary>
		/// <returns>The split status.</returns>
		/// <param name="job">Job.</param>
		/// <param name="split">Split.</param>
		SplitStatusMessage getSplitStatus(Guid job, int split);

		/// <summary>
		/// Shares the known workers.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="workers">Workers.</param>
		void shareKnownWorkers (string sender, HashSet<string> workers);

		/// <summary>
		/// Gets the load.
		/// </summary>
		/// <returns>The load.</returns>
		int getLoad();

		/// <summary>
		/// Signals the worker to cancel a split.
		/// </summary>
		/// <param name="job">Job.</param>
		/// <param name="split">Split.</param>
		void cancelSplit(Guid job, int split);
    }
}