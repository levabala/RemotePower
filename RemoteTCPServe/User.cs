using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public class User
    {
        public string publicKey;
        public string someInfo = "";

        public User(string publicKey)
        {
            this.publicKey = publicKey;
        }

        public User(string publicKey, string someInfo)
        {
            this.publicKey = publicKey;
            this.someInfo = someInfo;
        }
    }
}
