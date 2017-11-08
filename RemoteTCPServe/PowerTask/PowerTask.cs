using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTask
    {
        public int taskId;
        public string taskName;

        public PowerTask(string taskName)
        {
            taskId = -1;
            this.taskName = taskName;
        }

        public PowerTask(int taskId, string taskName)
        {
            this.taskId = taskId;
            this.taskName = taskName;            
        }        
    }    
}
