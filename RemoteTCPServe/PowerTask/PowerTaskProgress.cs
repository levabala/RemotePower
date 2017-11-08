using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskProgress : PowerTask
    {
        public double progress;

        public PowerTaskProgress(int taskId, string taskName, double progress)
            : base(taskId, taskName)
        {
            this.progress = progress;
        }
    }
}
