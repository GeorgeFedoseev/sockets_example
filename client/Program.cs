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

namespace client
{
    class Program : MyClient
    {
        static void Main(string[] args)
        {
            Program client = new Program();
            client.connnect();
            client.beginReceiving();
            client.send("Hello, server!");
            while (true) {
                string str = Console.ReadLine();
                client.send(str);
            }
        }

        protected override void receive(string message)
        {
            Console.WriteLine("From server: {0}", message);
        }
    }
}
