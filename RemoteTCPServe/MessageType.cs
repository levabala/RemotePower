using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    public enum MessageType
    {
        Greeting = 0,
        EndSession = 1,
        Error = 2,

        Auth = 20,
        AuthInit = 11,
        AuthInitResult = 12,
        AuthToken = 13,
        AuthTokenResult = 14,

        Task = 30,
        TaskInit = 21,
        TaskInitResult = 22,
        TaskProgress = 23,
        TaskResult = 24,
        TasksList = 25,
        TaskError = 26,
        TaskComplete = 27,       
       
        Registration = 40,
        RegistrationRequest = 31,
        RegistrationResult = 32
    }
}
