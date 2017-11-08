using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskIds : PowerTask
    {
        public int clientId;

        public PowerTaskIds(int taskId, string taskName, int clientId)
            : base(taskId, taskName)
        {
            this.clientId = clientId;
        }
    }
}
