using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using Sockets;

namespace server
{
    class Program : MyServer
    {
        static void Main(string[] args)
        {
            Program server= new Program();
            server.startServer();            
        }

        protected override void receive(Socket r_client, string message)
        {
 	        Console.WriteLine("From client: {0}", message);
            base.sendToAll("Hello, my clients!");
        }         
    }
}
