using RemoteTCPClient;
using RemoteTCPServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualBasic;

namespace ListModeClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MyClient client;
        int watchingTaskId = -1;
        string taskNameToRun = "";
        Dictionary<int, string> tasksStack = new Dictionary<int, string>();

        public MainWindow()
        {
            InitializeComponent();

            init();
        }

        public void init()
        {
            client = new MyClient();            
            if (!client.restoreConfiguration())
            {
                client.hostname = Interaction.InputBox("Enter your server ip", "Server IP request", "127.0.0.1");
                Int32.TryParse(Interaction.InputBox("Enter your server port number", "Server port request", "5555"), out client.port);
            }                            

            client.OnTasksListGot += Client_OnTasksListGot;
            client.OnPowerMessageProcessed += Client_OnPowerMessageProcessed;
            client.OnTaskInitialized += Client_OnTaskInitialized;
            client.OnTaskFinished += Client_OnTaskFinished;
            client.OnError += Client_OnError;            

            //ui events
            buttonRunTask.Click += ButtonRunTask_Click;

            //start
            client.init(/*type your server ip&port here*/);
        }

        private void Client_OnTasksListGot(object sender, PowerTasksDictionary dictionary)
        {
            //let's force it                        
            client.initTask("SummatorCPUTask", new object[] { @"-raw U:\2017\October\012616\012616_raw.001 -det 0".Split(' ') },
                (PowerTaskProgress t) =>
                {
                    runOnUIThread(() => { progressBarRunningTask.Value = t.progress; });
                },
                (PowerTaskResult t) =>
                {
                    
                },
                (PowerTaskResult t) =>
                {

                },
                (PowerTaskError t) =>
                {

                });
        }

        private void Client_OnError(object sender, string discription, Exception e)
        {
            runOnUIThread(() =>
            {
                textBoxErrors.Text += e.Message.ToString() + "\n\n";
                textBoxErrors.ScrollToEnd();
            });
        }

        private void ButtonRunTask_Click(object sender, RoutedEventArgs e)
        {
            if (!client.availableTasks.ContainsKey(taskNameToRun))
                return;

            client.initTask(taskNameToRun, new object[] { textBoxTaskArgs.Text.Split(' ') },
                (PowerTaskProgress t) =>
                {
                    runOnUIThread(() => { progressBarRunningTask.Value = t.progress; });
                },
                (PowerTaskResult t) =>
                {

                },
                (PowerTaskResult t) =>
                {

                },
                (PowerTaskError t) =>
                {

                });
        }

        private void Client_OnPowerMessageProcessed(object sender, PowerMessage mess)
        {
            runOnUIThread(() =>
            {
                switch (mess.messType)
                {
                    case MessageType.TasksList:                    
                        listBoxAvailableTasks.Items.Clear();
                        foreach (PowerTaskFunc taskFunc in client.availableTasks.Values)
                        {
                            ListBoxItem item = new ListBoxItem();
                            item.Content = taskFunc.taskName;
                            item.PreviewMouseDown += (it, arg) =>
                            {
                                textBoxChosenTaskToRun.Text = taskNameToRun = taskFunc.taskName;
                            };
                            listBoxAvailableTasks.Items.Add(item);
                        }                    
                        break;                    
                    case MessageType.TaskResult:
                        int valuesCount = 0;
                        int[][] spectr = ((int[][])(((PowerTaskResult)mess.value).result));
                        foreach (int[] arr in spectr)
                            valuesCount += arr.Length;
                        textBoxTaskOutput.Text += "Got " + valuesCount.ToString() + " values\n";
                        textBoxTaskOutput.ScrollToEnd();
                        break;
                }
            });
        }

        private void Client_OnTaskFinished(object sender, PowerTask task, PowerMessage mess, bool success)
        {
            tasksStack.Remove(task.taskId);
        }

        private void Client_OnTaskInitialized(object sender, PowerTaskFunc taskFunc, PowerTaskArgs taskArgs)
        {
            
        }

        private void runOnUIThread(Action action)
        {            
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(action);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            client.ShutDown();
            Application.Current.Shutdown();
        }
    }
}
