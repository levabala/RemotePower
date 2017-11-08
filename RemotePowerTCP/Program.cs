using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTCPServer;
using System.Configuration;

namespace RemoteTCPServerExample
{
    class Program
    {        
        static void Main(string[] args)
        {                        
            MyServer server = new MyServer(5081);
            server.restoreConfiguration();

            Console.WriteLine("Registered users");
            int index = 0;
            foreach (KeyValuePair<string, User> user in server.users)
                Console.WriteLine("{0}: {1}", index += 1, user.Value.someInfo);
            
            Console.ReadKey();            
        }        
    }
}
