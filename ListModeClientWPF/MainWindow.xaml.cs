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
        int watchingTaskId;
        string taskNameToRun;
        Dictionary<int, string> tasksStack;

        public MainWindow()
        {
            InitializeComponent();

            initClient();

            buttonRunTask.Click += ButtonRunTask_Click;
            buttonReconnect.Click += ButtonReconnect_Click;
        }

        public void initClient()
        {
            if (client != null)
                client.ShutDown();

            watchingTaskId = -1;
            taskNameToRun = "";
            tasksStack = new Dictionary<int, string>();

            client = new MyClient();            
            if (!client.restoreConfiguration())
            {
                InputDialog dialog = new InputDialog();
                dialog.ShowDialog();
                client.port = dialog.ServerPort;
                client.hostname = dialog.ServerIP;
            }
            client.saveConfiguration();

            client.OnTasksListGot += Client_OnTasksListGot;
            client.OnPowerMessageProcessed += Client_OnPowerMessageProcessed;
            client.OnTaskInitialized += Client_OnTaskInitialized;
            client.OnTaskFinished += Client_OnTaskFinished;
            client.OnError += Client_OnError;                        

            //start
            client.init(/*type your server ip&port here*/);
        }

        private void ButtonReconnect_Click(object sender, RoutedEventArgs e)
        {
            textBoxChosenRunningTask.Text =
                textBoxChosenTaskToRun.Text =
                textBoxErrors.Text =
                textBoxTaskArgs.Text =
                textBoxTaskOutput.Text =
                ""; // >_<

            listBoxAvailableTasks.Items.Clear();
            listBoxRunningTasks.Items.Clear();

            initClient();
        }

        private void Client_OnTasksListGot(object sender, Dictionary<string, PowerTaskFunc> dictionary)
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
                        PowerTaskResult res = (PowerTaskResult)mess.value;
                        int[][] spectr = ((int[][])(res).result);
                        foreach (int[] arr in spectr)
                            valuesCount += arr.Length;
                        tasksStack[res.taskId] += "Got " + valuesCount.ToString() + " values\n";
                        if (watchingTaskId == res.taskId)
                        {
                            textBoxTaskOutput.Text = tasksStack[res.taskId];
                            textBoxTaskOutput.ScrollToEnd();
                        }
                        break;
                }
            });
        }

        private void Client_OnTaskFinished(object sender, PowerTask task, PowerMessage mess, bool success)
        {
            tasksStack.Remove(task.taskId);
            if (watchingTaskId != task.taskId)
                return;

            if (tasksStack.Count == 0)
                watchingTaskId = -1;
            else
            {
                watchingTaskId = tasksStack.Keys.First();
                int index = 0;
                foreach (var item in listBoxRunningTasks.Items)
                {
                    if (item.ToString().Split(':').Skip(2).ToString() == watchingTaskId.ToString()) //костыыыыыыыыыыыль
                        listBoxRunningTasks.SelectedIndex = index;
                    index++;
                }
            }
        }

        private void Client_OnTaskInitialized(object sender, PowerTaskFunc taskFunc, PowerTaskArgs taskArgs)
        {
            tasksStack.Add(taskArgs.taskId, taskArgs.taskName + "\n");

            runOnUIThread(() =>
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = "id" + taskArgs.taskId.ToString() + ": " + taskArgs.taskName;
                item.PreviewMouseDown += (it, arg) =>
                {
                    textBoxChosenTaskToRun.Text = taskArgs.taskName;
                    watchingTaskId = taskArgs.taskId;
                    textBoxTaskOutput.Text = tasksStack[taskArgs.taskId];
                };
                listBoxRunningTasks.Items.Add(item);

                if (watchingTaskId == -1)
                {
                    watchingTaskId = taskArgs.taskId;
                    listBoxRunningTasks.SelectedIndex = 0;
                }
            });
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
