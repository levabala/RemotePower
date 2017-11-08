using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskResult : PowerTask
    {
        public object result;

        public PowerTaskResult(int taskId, string taskName, object result)
            : base(taskId, taskName)
        {
            this.result = result;
        }
    }
}
