using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;

namespace PADIMapNoReduce
{
    public class PuppetMasterService : MarshalByRefObject, IPuppetMasterService
    {
        Controller _controller;

         public PuppetMasterService(int port, Controller controller)
        {
            _controller = controller;
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(this, "PM", typeof(PuppetMasterService));
        }

         public override object InitializeLifetimeService()
         {
             return null;
         }

         public void createWorker(int port, string otherWorkers)
         {
             _controller.createWorker(port, otherWorkers);
         }
         
         public void createWorker(string url, string otherWorkers)
         {
             _controller.createWorker(url, otherWorkers);
         }
    }
}
