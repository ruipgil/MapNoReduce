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
    class ClientMain
    {
        static void Main(string[] args)
        {
            // Clients should be between 10001 and 19999
            // Workers are between 30001 and 39999
            ClientService client = new ClientService(10001);
            string workerAddress = "tcp://localhost:30001/W";
            client.init(workerAddress);
            client.submit(@"./input.txt", 4);
            //Console.In.ReadLine();
        }
    }
}
