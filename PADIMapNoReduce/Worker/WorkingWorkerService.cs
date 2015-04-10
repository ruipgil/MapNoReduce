using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;

namespace PADIMapNoReduce
{
    class WorkingWorkerService : WorkerService, IWorkingWorkerService
    {
        // The workers should be between 30001 and 39999, but what about the inner workers?
        public WorkingWorkerService(int port)
        {
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "W", typeof(WorkingWorkerService));
        }

        public void work(int start, int end, int split, string clientUrl,byte[] code, string mapperName)
        {
            Console.Out.WriteLine("#work "+start+" "+end+" "+split+" "+clientUrl);
            // open connection with client, through clientUrl
            IClientService client = (IClientService)Activator.GetObject(typeof(IClientService), clientUrl);
            // ask for data from start to end
            List<string> values = client.get(start, end);

            List<IList<KeyValuePair<string, string>>> result = processDataWithMapper(code, mapperName, values);

            // send to client
            client.set(split, result);
        }

        private List<IList<KeyValuePair<string, string>>> processDataWithMapper(byte[] code, string mapperName, List<string> values)
        {
            List<IList<KeyValuePair<string, string>>> result = new List<IList<KeyValuePair<string, string>>>();
            
            object ClassObj = new object();
            Assembly assembly = Assembly.Load(code);

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (type.FullName.EndsWith("." + mapperName))
                    {
                        ClassObj = Activator.CreateInstance(type);
                        // process data with mapper
                        foreach (string value in values)
                        {
                            object[] args = new object[] { value };
                            object resultObject = type.InvokeMember("Map",
                            BindingFlags.Default | BindingFlags.InvokeMethod,
                               null,
                               ClassObj,
                               args);
                            IList<KeyValuePair<string, string>> mapResult = (IList<KeyValuePair<string, string>>)resultObject;
                            result.Add(mapResult);
                        }
                    }
                }
            }
            return result;
        }
    }
}
