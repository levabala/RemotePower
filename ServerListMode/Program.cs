using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTCPServer;

namespace ServerListMode
{
    class Program
    {
        static void Main(string[] args)
        {
            MyServer server = new MyServer(5081);
            server.restoreConfiguration();            

            Console.ReadKey();
        }
    }
}
