using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public enum Details
    {
        None = 0,        
        Accepted = 1,
        Forbiden = 2,
        NoSuchUser = 3,
        UserExists = 4,
        AccessDenied = 5,
        Authorised = 6,
        AuthFailed = 7,
        NoSuchTask = 8,
        OK = 9,
        AlreadyRegistered = 10,
        Success = 11,
    }
}
