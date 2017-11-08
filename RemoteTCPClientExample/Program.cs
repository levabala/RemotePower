using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTCPClient;
using System.Security.Cryptography;
using System.Configuration;
using System.Collections.Specialized;
using RemoteTCPServer;
using System.Threading;
using System.Globalization;

namespace RemoteTCPClientExample
{
    class Program
    {
        static void Main(string[] args)
        {                                              
            MyClient client = new MyClient();
            client.restoreConfiguration();
            client.init("159.93.101.156", 5081);                        

            Console.ReadKey();
        }
    }
}
