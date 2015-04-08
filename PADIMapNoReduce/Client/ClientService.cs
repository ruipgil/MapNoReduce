﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;

namespace PADIMapNoReduce
{
    public class ClientService : MarshalByRefObject, IClientService
    {
        Dictionary<string, string> originalKeyVal = new Dictionary<string, string>();
        Dictionary<string, string> processedKeyVal = new Dictionary<string, string>();

        TcpChannel channel;
        IWorkerService knownWorker;

        List<string> file;
        int lines;

        public ClientService(string workerEntryUrl)
        {
            channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
            knownWorker = (IWorkerService)Activator.GetObject(typeof(IWorkerService), workerEntryUrl);
        }

        public void submit(string inputFile, int splits) // incomplete
        {
            file = new List<string>(File.ReadAllLines(inputFile));
            lines = file.Count();
            knownWorker.submit(lines, splits);
        }

        public List<string> get(int start, int end)
        {
            return file.GetRange(start, end - start);
        }
    }
}
