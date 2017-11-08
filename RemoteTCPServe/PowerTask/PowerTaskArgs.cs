using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskArgs : PowerTask
    {        
        public object[] args;

        public PowerTaskArgs(int taskId, string taskName, object[] args)
            : base(taskId, taskName)
        {            
            this.args = args;
        }
    }
}
