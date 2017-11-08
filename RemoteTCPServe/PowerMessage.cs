using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServer
{
    [Serializable]
    public class PowerMessage
    {
        public MessageType messType;
        public Details details;
        public object value;

        public PowerMessage(MessageType messType, Details details = Details.None)
        {
            this.details = details;
            this.messType = messType;            
        }

        public PowerMessage(MessageType messType, object value, Details details = Details.None)
        {
            this.details = details;
            this.messType = messType;
            this.value = value;
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
            catch (Exception e)
            {
                
            }
        }

        public static PowerMessage Deserialize(byte[] obj)
        {
            using (var memoryStream = new MemoryStream(obj))
                return (PowerMessage)(new BinaryFormatter()).Deserialize(memoryStream);
        }

        public static PowerMessage Deserialize(NetworkStream stream)
        {
            return (PowerMessage)new BinaryFormatter().Deserialize(stream);            
        }
    }
}
