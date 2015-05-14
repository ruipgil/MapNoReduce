using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    public interface IRemoteTesting
    {
        /// <summary>
        /// Freezes the worker.
        /// </summary>
        void freezeWorker();

        /// <summary>
        /// Unfreezes the worker.
        /// </summary>
        void unfreezeWorker();

        /// <summary>
        /// Freezes the coordinator.
        /// </summary>
        void freezeCoordinator();

        /// <summary>
        /// Unfreezes the coordinator.
        /// </summary>
        void unfreezeCoordinator();

        /// <summary>
        /// Slows the worker.
        /// </summary>
        /// <param name="seconds">Seconds.</param>
        void slowWorker(int seconds);

        /// <summary>
        /// Gets the status.
        /// </summary>
        void getStatus();
    }
}
