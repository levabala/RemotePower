using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RemoteTCPServer;
using System.Threading;
using System.Globalization;
using System.Configuration;

namespace RemoteTCPClient
{
    public class Client
    {
        public delegate void AuthFinishedHandler(object sender, bool success);
        public event AuthFinishedHandler OnAuthFinished;

        public delegate void PowerMessageProcessedHandler(object sender, PowerMessage mess);
        public event PowerMessageProcessedHandler OnPowerMessageProcessed;

        public delegate void TasksListGotHandler(object sender, PowerTasksDictionary dictionary);
        public event TasksListGotHandler OnTasksListGot;

        public delegate void TaskFinishedHandler(object sender, PowerTask task, PowerMessage mess, bool success);
        public event TaskFinishedHandler OnTaskFinished;

        public delegate void TaskInitializedHandler(object sender, PowerTaskFunc taskFunc, PowerTaskArgs taskArgs);
        public event TaskInitializedHandler OnTaskInitialized;

        public delegate void ErrorHandler(object sender, string discription, Exception e);
        public event ErrorHandler OnError;


        public PowerTasksDictionary availableTasks = new PowerTasksDictionary();
        public Dictionary<int, Action<int>> initTasksCallback = new Dictionary<int, Action<int>>();
        public Dictionary<int, Action<PowerTaskResult>> runningTasksComplete = new Dictionary<int, Action<PowerTaskResult>>();
        public Dictionary<int, Action<PowerTaskResult>> runningTasksResult= new Dictionary<int, Action<PowerTaskResult>>();
        public Dictionary<int, Action<PowerTaskProgress>> runningTasksProgress = new Dictionary<int, Action<PowerTaskProgress>>();
        public Dictionary<int, Action<PowerTaskError>> runningTasksError = new Dictionary<int, Action<PowerTaskError>>();

        protected TcpClient client;
        protected NetworkStream stream;
        protected RSACryptoServiceProvider provider;        
        protected string xmlPublicKey;
        public bool sessionActive = true;
        public bool authorized = false;
        public string hostname = "127.0.0.1";
        public int port = 0;

        private Thread listneingLoopThread;

        public Client()
        {
            provider = new RSACryptoServiceProvider(512);
            xmlPublicKey = provider.ToXmlString(false);
        }

        public Client(string xmlRsaParams)
        {            
            provider = new RSACryptoServiceProvider(512);            
            provider.FromXmlString(xmlRsaParams);
            xmlPublicKey = provider.ToXmlString(false);            
        }        

        public void ShutDown()
        {
            if (sessionActive)
            {
                new PowerMessage(MessageType.EndSession).Serialize(stream);
                client.Close();
                stream.Close();
                sessionActive = false;
            }
            listneingLoopThread.Abort();
        }

        public void init()
        {
            OnError += (a,b,c) => { };            

            if (client != null)
                client.Close();
            if (stream != null)
                stream.Close();
            try
            {
                client = new TcpClient(hostname, port);
                stream = client.GetStream();
            }
            catch (Exception e)
            {
                OnError(this, "It seems like server is offline", e);                
                return;
            }

            greet();
            requestAuthentication();

            listneingLoopThread = new Thread(() =>
            {
                launchListeningLoop();
            });
            listneingLoopThread.Start();
        }

        public void init(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
            init();
            saveConfiguration();
        }

        public void clearConfiguration()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);            
            conf.AppSettings.Settings.Remove("RSAParams");
            conf.Save(ConfigurationSaveMode.Full);
        }

        public void saveConfiguration()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var rsaParamsXML = conf.AppSettings.Settings["RSAParams"];            
            string privateKeyXml = provider.ToXmlString(true);
            if (rsaParamsXML == null)
                conf.AppSettings.Settings.Add("RSAParams", privateKeyXml);
            else
                conf.AppSettings.Settings["RSAParams"].Value = privateKeyXml;

            var hostnameParam = conf.AppSettings.Settings["Hostname"];            
            if (hostnameParam == null)
                conf.AppSettings.Settings.Add("Hostname", hostname);
            else
                conf.AppSettings.Settings["Hostname"].Value = hostname;

            var portParam = conf.AppSettings.Settings["Port"];
            if (portParam == null)
                conf.AppSettings.Settings.Add("Port", port.ToString());
            else
                conf.AppSettings.Settings["Port"].Value = port.ToString();

            conf.Save(ConfigurationSaveMode.Full);
        }

        public void restoreConfiguration()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var rsaParamsXML = conf.AppSettings.Settings["RSAParams"];
            if (rsaParamsXML != null)
            {                
                provider.FromXmlString(rsaParamsXML.Value);
                xmlPublicKey = provider.ToXmlString(false);
            }
            else conf.AppSettings.Settings.Add("RSAParams", provider.ToXmlString(true));

            var hostnameParam = conf.AppSettings.Settings["Hostname"];
            if (hostnameParam == null)
                conf.AppSettings.Settings.Add("Hostname", hostname);
            else
                hostname = conf.AppSettings.Settings["Hostname"].Value;

            var portParam = conf.AppSettings.Settings["Port"];
            if (portParam == null)
                conf.AppSettings.Settings.Add("Port", port.ToString());
            else
                port = Int32.Parse(conf.AppSettings.Settings["Port"].Value);

            conf.Save(ConfigurationSaveMode.Full);
        }

        public void launchListeningLoop()
        {            
            while (sessionActive)
            {
                PowerMessage mess;
                try
                {
                    mess = PowerMessage.Deserialize(stream);                    
                }
                catch(IOException e)
                {
                    OnError(this, "The server gone away..", e);                    
                    return;
                }
                catch(Exception e)
                {
                    OnError(this, "We have a problem, Houston.. But we continue", e);                    
                    continue;
                }

                switch (mess.messType)
                {
                    case MessageType.Greeting:
                        //greet();                        
                        break;
                    case MessageType.EndSession:
                        sessionActive = false;
                        break;
                    case MessageType.AuthInitResult:
                        if (mess.details != Details.UserExists)
                            new PowerMessage(MessageType.RegistrationRequest, xmlPublicKey).Serialize(stream);                        
                        break;
                    case MessageType.RegistrationResult:
                        if (mess.details == Details.AccessDenied)
                            throw new Exception("Registration refused");
                        new PowerMessage(MessageType.AuthInit, xmlPublicKey).Serialize(stream);
                        break;
                    case MessageType.AuthToken:
                        byte[] token = (byte[])mess.value;
                        byte[] signedToken = provider.SignData(token, "SHA256");
                        new PowerMessage(MessageType.AuthToken, signedToken).Serialize(stream);
                        break;
                    case MessageType.AuthTokenResult:
                        if (mess.details == Details.Authorised)
                        {
                            authorized = true;
                            new PowerMessage(MessageType.TasksList).Serialize(stream);
                        }

                        if (OnAuthFinished != null)
                            OnAuthFinished(this, authorized);
                        
                        break;
                    case MessageType.TasksList:
                        if (mess.details != Details.OK)
                            break;

                        availableTasks = (PowerTasksDictionary)mess.value;
                        OnTasksListGot(this, availableTasks);
                        break;
                    case MessageType.TaskInitResult:
                        if (mess.details != Details.Accepted)
                            throw new Exception("Task " + ((int)mess.value).ToString() + " wasn't accepted");

                        PowerTaskIds ids = (PowerTaskIds)mess.value;

                        if (initTasksCallback.ContainsKey(ids.clientId))
                        {
                            initTasksCallback[ids.clientId](ids.taskId);
                            //Console.WriteLine("Task \"{2}{0}\" initialized as \"{2}{1}\" on server", ids.clientId, ids.taskId, ids.taskName);
                        }

                        break;
                    case MessageType.TaskError:
                        PowerTaskError taskError = (PowerTaskError)mess.value;
                        if (runningTasksError.ContainsKey(taskError.taskId))
                            runningTasksError[taskError.taskId](taskError);
                        if (taskError.fatal)
                            OnTaskFinished(this, taskError, mess, false);
                        break;
                    case MessageType.TaskProgress:
                        PowerTaskProgress taskProgress = (PowerTaskProgress)mess.value;
                        if (runningTasksProgress.ContainsKey(taskProgress.taskId))
                            runningTasksProgress[taskProgress.taskId](taskProgress);
                        break;
                    case MessageType.TaskComplete:
                        PowerTaskResult taskComplete = (PowerTaskResult)mess.value;
                        if (runningTasksComplete.ContainsKey(taskComplete.taskId))
                            runningTasksComplete[taskComplete.taskId](taskComplete);
                        OnTaskFinished(this, taskComplete, mess, false);
                        break;
                    case MessageType.TaskResult:
                        PowerTaskResult taskResult = (PowerTaskResult)mess.value;
                        if (runningTasksResult.ContainsKey(taskResult.taskId))
                            runningTasksResult[taskResult.taskId](taskResult);
                        break;
                }

                OnPowerMessageProcessed(this, mess);
            }
        }

        protected void greet()
        {
            new PowerMessage(MessageType.Greeting).Serialize(stream);
        }

        public void requestAuthentication()
        {            
            new PowerMessage(MessageType.AuthInit, xmlPublicKey).Serialize(stream);
        }

        public void initTask(
            string taskName, object[] args,
            Action<PowerTaskProgress> taskProgress, Action<PowerTaskResult> taskResult, Action<PowerTaskResult> taskCompleted,
            Action<PowerTaskError> taskError)
        {
            if (!authorized)
                throw new Exception("The client wasn't authorized");
            if (!availableTasks.ContainsKey(taskName))
                throw new Exception("No such task");

            int id = 0;
            while (initTasksCallback.ContainsKey(id))
                id++;

            initTasksCallback[id] = (idd) =>
            {
                runningTasksResult[idd] = taskResult;
                runningTasksComplete[idd] = taskCompleted;
                runningTasksError[idd] = taskError;
                runningTasksProgress[idd] = taskProgress;
            };


            PowerTaskArgs taskArgs = new PowerTaskArgs(id, taskName, args);

            OnTaskInitialized(this, availableTasks[taskName], taskArgs);
            new PowerMessage(MessageType.TaskInit, taskArgs).Serialize(stream);
        }
    }
}
