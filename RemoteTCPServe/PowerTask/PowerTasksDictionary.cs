using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerTasksDictionary : Dictionary<string, PowerTaskFunc>
    {                
        public PowerTasksDictionary()
        {
            
        }

        public PowerTasksDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {

        }

        public PowerTasksDictionary(PowerTaskFunc[] funcs)
        {
            foreach (PowerTaskFunc func in funcs)
                Add(func);
        }

        public PowerTasksDictionary(Dictionary<string, PowerTaskFunc> funcs)
        {            
            foreach (PowerTaskFunc func in funcs.Values)
                Add(func);
        }        

        public PowerTasksDictionary Add(PowerTaskFunc func)
        {
            Add(func.taskName, func);
            return this;
        }

        public byte[] Serialize()
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public void Serialize(NetworkStream stream)
        {
            try
            {
                new BinaryFormatter().Serialize(stream, this);
            }
            catch (IOException e)
            {

            }
        }

        public static PowerTasksDictionary Deserialize(byte[] obj)
        {
            using (var memoryStream = new MemoryStream(obj))
                return (PowerTasksDictionary)(new BinaryFormatter()).Deserialize(memoryStream);
        }

        public static PowerTasksDictionary Deserialize(NetworkStream stream)
        {
            return (PowerTasksDictionary)new BinaryFormatter().Deserialize(stream);
        }
    }
}
