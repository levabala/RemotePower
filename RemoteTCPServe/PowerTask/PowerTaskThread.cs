using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskThread : PowerTask
    {
        public Thread thread;

        public PowerTaskThread(int taskId, string taskName, Thread thread)
            : base(taskId, taskName)
        {
            this.thread = thread;
        }
    }
}
