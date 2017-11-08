using RemoteTCPClient;
using RemoteTCPServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListModeClientWPF
{
    class MyClient : Client
    {        
        public MyClient()
            : base()
        {
            OnPowerMessageProcessed += MyClient_OnPowerMessageProcessed;
            OnAuthFinished += MyClient_OnAuthFinished;
        }

        private void MyClient_OnAuthFinished(object sender, bool success)
        {
            /*initTask("GetFilesListTask", new object[] { @".\" }, (taskProgress) =>
            {
                Console.WriteLine("{0}: {1}%", taskProgress.taskId, taskProgress.progress);
            }, (taskResult) => { }
             , (taskComplete) =>
             {
                 Console.WriteLine("{0}: Completed", taskComplete.taskId);
                 string[] result = (string[])taskComplete.completeResult;
                 foreach (string s in result)
                     Console.WriteLine("\t{0}", s);
             }, (taskError) =>
             {
                 Console.WriteLine("{0}: {1}", taskError.taskId, taskError.ToString());
             });*/
        }

        private void MyClient_OnPowerMessageProcessed(object sender, PowerMessage mess)
        {
            Console.WriteLine("{0, -20}: {1}", mess.messType, mess.details);
            switch (mess.messType)
            {
                case MessageType.TasksList:
                    foreach (PowerTaskFunc powerFunc in availableTasks.Values)
                        Console.WriteLine("\t{0}: {1}", powerFunc.taskName, powerFunc.discription);
                    break;
            }
        }
    }
}
