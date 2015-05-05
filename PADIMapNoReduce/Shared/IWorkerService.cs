using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce
{
    /// <summary>
    /// Worker interface visible to the client
    /// </summary>
    public interface IWorkerService
    {
		void submit(string clientAddress, int inputSize, int splits, byte[] code, string mapperName);
		/*
        bool getFreeze();

        void freezeWorker();

        void unFreezeWorker();

        void slowWorker(int seconds);

        string getStatus();
        */
    }
}
