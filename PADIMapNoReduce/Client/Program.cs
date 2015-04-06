﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapNoReduce
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel();
            IWorker server = (IWorker)Activator.GetObject(typeof(IWorker),
                "tcp://localhost:10000/MapNoReduceWorker");
        }
    }
}
