using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskError : PowerTask
    {        
        public bool fatal;
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public PowerTaskError(int taskId, string taskName, bool fatal, Exception exc)
            : base(taskId, taskName)
        {            
            this.fatal = fatal;            
            Message = exc.Message;
            StackTrace = exc.StackTrace;
            TimeStamp = DateTime.Now;
        }

        public override string ToString()
        {
            return Message + StackTrace;
        }
    }
}
