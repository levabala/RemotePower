using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RemoteTCPServer
{
    public abstract class Server
    {
        public delegate void UserRegisteredHandler(object sender, User user);
        public event UserRegisteredHandler OnUserRegisteredFinished;

        public static Dictionary<string, PowerTaskFunc> availableTasks;        
        public Dictionary<int, PowerTaskThread> runningTasks = new Dictionary<int, PowerTaskThread>();
        public Dictionary<string, User> users;
        public Dictionary<string, Dictionary<int, PowerTaskThread>> usersTasks = new Dictionary<string, Dictionary<int, PowerTaskThread>>();

        protected readonly int port;
        protected TcpListener server;
        protected Func<string, string, bool> registrate;
        
        public Server(int port)
        {
            OnUserRegisteredFinished += (a, b) => { };

            availableTasks = CreateTasks();

            this.port = port;            
            registrate = defaultRegistration;
            users = new Dictionary<string, User>();

            IPAddress localAddr = IPAddress.Any;
            server = new TcpListener(localAddr, port);
            server.Start();

            Console.WriteLine("Server started on {0}:{1}", localAddr, port);

            new Thread(() =>
            {
                listeningLoop();
                server.Stop();
            }).Start();
        }

        public Server(int port, Dictionary<string, User> users)
            : this(port)
        {
            this.users = users;            
        }

        public Server(int port, Func<string, string, bool> registrate)
            : this(port)
        {            
            this.registrate = registrate;
        }

        public Server(int port, Dictionary<string, User> users, Func<string, string, bool> registrate)
            : this(port)
        {
            this.users = users;
            this.registrate = registrate;
        }

        public static bool defaultRegistration(string key, string ip)
        {
            Console.WriteLine("{0} wants to be registered\nAccept? Y/N", ip);
            string answ = Console.ReadLine();
            if (answ.ToLower().Replace(' ', '\0') == "y")
            {
                Console.WriteLine("Accepted");
                return true;
            }
            else
            {
                Console.WriteLine("Refused");
                return false;
            }
        }

        public void saveConfiguration()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationElement usersElem = conf.AppSettings.Settings["Users"];
            if (usersElem == null)            
                conf.AppSettings.Settings.Add("Users", dictionaryToString(users));
            else 
                conf.AppSettings.Settings["Users"].Value = dictionaryToString(users);

            conf.Save(ConfigurationSaveMode.Full);
        }

        public void restoreConfiguration()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationElement usersElem = conf.AppSettings.Settings["Users"];
            if (usersElem == null)
                conf.AppSettings.Settings.Add("Users", dictionaryToString(users));
            else
                users = dictionaryFromString(conf.AppSettings.Settings["Users"].Value);
            conf.Save(ConfigurationSaveMode.Full);

            foreach (User user in users.Values)
                usersTasks[user.publicKey] = new Dictionary<int, PowerTaskThread>();
        }

        private string dictionaryToString(Dictionary<string, User> dictionary)
        {
            XElement xElem = new XElement(
                    "items",
                    dictionary.Select(
                        x => new XElement("item", new XAttribute("id", x.Key), new XAttribute("value", x.Value.someInfo))
                        )
                 );
            return xElem.ToString();
        }

        private Dictionary<string, User> dictionaryFromString(string str)
        {
            try
            {
                XElement xElem2 = XElement.Parse(str); //XElement.Load(...)
                Dictionary<string, User> newDict = xElem2.Descendants("item")
                                    .ToDictionary(
                        x => (string)x.Attribute("id"), x => new User((string)x.Attribute("id"), (string)x.Attribute("value"))
                    );
                return newDict;
            }
            catch(Exception e)
            {
                Console.WriteLine("There were an interesting exception while we had been restoring users list..");
                Console.WriteLine("So we've just deleted everything :)");
                return new Dictionary<string, User>();
            }            
        }

        private void listeningLoop()
        {
            try
            {
                while (true)
                {
                    //Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    new Thread(() =>
                    {
                        clientProccessing(client);
                    }).Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void clientProccessing(TcpClient client)
        {
            Byte[] bytes = new Byte[256];            

            Console.WriteLine("Connected!");            

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            bool sessionActive = true;
            bool authorized = false;
            IPEndPoint clientIp = (IPEndPoint)client.Client.RemoteEndPoint;
            User user = null;
            RSACryptoServiceProvider provider = new RSACryptoServiceProvider(512);
            byte[] token = new byte[0];            
            while (sessionActive)
            {
                try
                {
                    PowerMessage mess;
                    try
                    {
                        mess = PowerMessage.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Message deserialize error. Connection was shut down");
                        client.Close();
                        stream.Close();
                        return;
                    }
                    switch (mess.messType)
                    {
                        case MessageType.Greeting:
                            new PowerMessage(MessageType.Greeting).Serialize(stream);
                            break;
                        case MessageType.EndSession:
                            Console.WriteLine(clientIp.ToString() + " disconnected");
                            sessionActive = false;
                            break;
                        case MessageType.AuthInit:
                            //checking if user registered
                            string publicKeyXML = (string)mess.value;                            

                            PowerMessage resMess = new PowerMessage(MessageType.AuthInitResult, Details.NoSuchUser);
                            if (users.ContainsKey(publicKeyXML))
                            {
                                provider.FromXmlString(publicKeyXML);
                                user = users[publicKeyXML];
                                resMess.details = Details.UserExists;

                                //sending token                        
                                token = Guid.NewGuid().ToByteArray();
                                new PowerMessage(MessageType.AuthToken, token).Serialize(stream);
                            }
                            resMess.Serialize(stream);                            
                            break;
                        case MessageType.AuthToken:
                            //verify token
                            byte[] sign = (byte[])mess.value;
                            bool success = authorized = provider.VerifyData(token, "SHA256", sign);

                            Details details = (success) ? Details.Authorised : Details.AuthFailed;
                            new PowerMessage(MessageType.AuthTokenResult, 0, details).Serialize(stream);
                            break;
                        case MessageType.RegistrationRequest:
                            string pkey = (string)mess.value;
                            if (users.ContainsKey(pkey))
                            {
                                new PowerMessage(MessageType.RegistrationResult, Details.AlreadyRegistered).Serialize(stream);
                                break;
                            }

                            bool toRegistrate = registrate(pkey, clientIp.ToString());
                            if (toRegistrate)
                            {
                                provider.FromXmlString(pkey);                                
                                users[pkey] = new User(pkey);
                                usersTasks[pkey] = new Dictionary<int, PowerTaskThread>();
                                new PowerMessage(MessageType.RegistrationResult, Details.Success).Serialize(stream);

                                OnUserRegisteredFinished(this, users[pkey]);
                                saveConfiguration();
                            }
                            else new PowerMessage(MessageType.RegistrationResult, Details.AccessDenied).Serialize(stream);

                            break;
                        case MessageType.TaskInit:
                            //task searching
                            PowerTaskArgs taskArgs = (PowerTaskArgs)mess.value;
                            initTask(taskArgs, stream, user);

                            break;
                        case MessageType.TasksList:
                            if (!authorized)
                            {
                                new PowerMessage(MessageType.TasksList, Details.AccessDenied).Serialize(stream);
                                break;
                            }

                            new PowerMessage(MessageType.TasksList, availableTasks, Details.OK).Serialize(stream);
                            break;
                    }
                }
                catch(Exception e)
                {
                    new PowerMessage(MessageType.Error, e).Serialize(stream);
                    continue;
                }
            }                        

            client.Close();
        }        

        private void initTask(PowerTaskArgs taskArgs, NetworkStream stream, User user)
        {
            if (!availableTasks.ContainsKey(taskArgs.taskName))
            {
                new PowerMessage(MessageType.TaskInitResult, taskArgs.taskId, Details.NoSuchTask).Serialize(stream);
                return;
            }

            Action<PowerTaskProgress> progressCallback = (taskpower) =>
            {
                new PowerMessage(MessageType.TaskProgress, taskpower).Serialize(stream);
            };
            Action<PowerTaskResult> resultCallback = (taskpower) =>
            {
                //runningTasks.Remove(taskpower.taskId);
                new PowerMessage(MessageType.TaskResult, taskpower).Serialize(stream);
            };
            Action<PowerTaskResult> completeCallback = (taskpower) =>
            {
                runningTasks.Remove(taskpower.taskId);
                usersTasks[user.publicKey].Remove(taskpower.taskId);
                new PowerMessage(MessageType.TaskComplete, taskpower).Serialize(stream);
            };
            Action<PowerTaskError> errorCallback = (taskpower) =>
            {
                if (taskpower.fatal)
                {
                    runningTasks.Remove(taskpower.taskId);
                    usersTasks[user.publicKey].Remove(taskpower.taskId);
                }
                new PowerMessage(MessageType.TaskError, taskpower).Serialize(stream);
            };

            int id = 0;
            while (runningTasks.ContainsKey(id))
                id++;

            PowerTaskThread powerThread = new PowerTaskThread(id, taskArgs.taskName, new Thread(() =>
            {
                PowerTaskFunc powerTaskFunc = availableTasks[taskArgs.taskName];
                try
                {
                    powerTaskFunc.func(taskArgs, progressCallback, resultCallback, completeCallback, errorCallback);
                }
                catch (Exception e)
                {
                    errorCallback(new PowerTaskError(id, taskArgs.taskName, true, e));
                }
            }));
            runningTasks[id] = powerThread;
            usersTasks[user.publicKey][powerThread.taskId] = powerThread;
            powerThread.thread.Start();

            //new int[] {task.taskId, id} - here we put 1st is ClientTaskId and 2nd is ServerTaskId
            new PowerMessage(MessageType.TaskInitResult, new PowerTaskIds(id, taskArgs.taskName, taskArgs.taskId), Details.Accepted).Serialize(stream);
        }

        protected abstract PowerTasksDictionary CreateTasks();

        public class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] left, byte[] right)
            {
                if (left == null || right == null)
                {
                    return left == right;
                }
                if (left.Length != right.Length)
                {
                    return false;
                }
                for (int i = 0; i < left.Length; i++)
                {
                    if (left[i] != right[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public int GetHashCode(byte[] key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                int sum = 0;
                foreach (byte cur in key)
                {
                    sum += cur;
                }
                return sum;
            }
        }
    }    
}
