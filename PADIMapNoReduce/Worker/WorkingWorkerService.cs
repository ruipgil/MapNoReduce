using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace PADIMapNoReduce
{
    class WorkingWorkerService : WorkerService, IWorkingWorkerService
    {
        // The workers should be between 30001 and 39999, but what about the inner workers?
        public WorkingWorkerService(int port)
        {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(this, "W", typeof(WorkingWorkerService));
        }

        public void work(int start, int end, int split, string clientUrl)
        {
            Console.Out.WriteLine("#work "+start+" "+end+" "+split+" "+clientUrl);
            // open connection with client, through clientUrl
            IClientService client = (IClientService)Activator.GetObject(typeof(IClientService), clientUrl);
            // ask for data from start to end
            List<string> values = client.get(start, end);
            List<IList<KeyValuePair<string, string>>> result = new List<IList<KeyValuePair<string, string>>>();

            IMapper mapper = new SampleMapper();
            // process data with mapper
            foreach (string value in values)
            {
                IList<KeyValuePair<string, string>> mapResult = mapper.Map(value);
                result.Add(mapResult);
            }

            // send to client
            client.set(split, result);
        }
    }
}
