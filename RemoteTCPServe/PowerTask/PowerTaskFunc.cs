using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTaskFunc : PowerTask
    {
        [NonSerialized]
        public Action<PowerTaskArgs, Action<PowerTaskProgress>, Action<PowerTaskResult>, Action<PowerTaskResult>, Action<PowerTaskError>> func;
        public Type outputType;
        public Type[] inputTypes;
        public readonly string discription;

        public PowerTaskFunc(
            string taskName,
            Action<PowerTaskArgs, Action<PowerTaskProgress>, Action<PowerTaskResult>, Action<PowerTaskResult>, Action<PowerTaskError>> func,
            Type outputType,
            Type[] inputTypes,
            string discription = "")
            //: this(taskName, func, discription)
            : base(taskName)
        {
            this.func = func;
            this.discription = discription;
            this.outputType = outputType;
            this.inputTypes = inputTypes;
        }

        public PowerTaskFunc(
            string taskName,
            Action<PowerTaskArgs, Action<PowerTaskProgress>, Action<PowerTaskResult>, Action<PowerTaskResult>, Action<PowerTaskError>> func,
            Type outputType,
            Type inputType,
            string discription = "")
            //: this(taskName, func, discription)
            : base(taskName)
        {
            this.func = func;
            this.discription = discription;
            this.outputType = outputType;
            inputTypes = new Type[] { inputType };
        }

        public PowerTaskFunc(
            string taskName,
            Action<PowerTaskArgs, Action<PowerTaskProgress>, Action<PowerTaskResult>, Action<PowerTaskResult>, Action<PowerTaskError>> func,
            Type outputType,            
            string discription = "")
            //: this(taskName, func, discription)
            : base(taskName)
        {
            this.func = func;
            this.discription = discription;
            this.outputType = outputType;
            inputTypes = new Type[0];
        }
    }
}
