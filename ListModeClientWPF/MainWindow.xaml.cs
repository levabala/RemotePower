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
            //client.clearConfiguration();
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
            client.OnDirectoryGot += Client_OnDirectoryGot;            

            //start
            client.init(/*type your server ip&port here*/);
        }

        private void Client_OnDirectoryGot(object sender, bool success)
        {
            treeViewServerFileTree.Items.Clear();
            TreeViewItem errorItem = new TreeViewItem();
            errorItem.Header = "Invalid or non-existing path";
            treeViewServerFileTree.Items.Add(errorItem);
            if (!success)
            {
                
                return;
            }
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
            //initTask("SummatorCPUTask", @"-raw U:\2017\October\012616\012616_raw.001 -det 0".Split(' '));            
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
            initTask(taskNameToRun, textBoxTaskArgs.Text.Split(' '));
        }

        private void initTask(string taskName, object[] args)
        {
            if (!client.availableTasks.ContainsKey(taskName))
                return;

            client.initTask(taskNameToRun, args,
                (t) =>
                {
                    
                },
                (t) =>
                {

                },
                (t) =>
                {

                },
                (t) =>
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
                    case MessageType.TaskProgress:
                        PowerTaskProgress progress = (PowerTaskProgress)mess.value;
                        if (watchingTaskId == progress.taskId)
                            progressBarRunningTask.Value = progress.progress * 100;
                        break;
                    default:
                        bool itIs = mess.value is PowerTask;
                        if (itIs) {
                            PowerTask task = (PowerTask)mess.value;
                            Console.WriteLine("{0}: {1} | {2}", mess.messType, mess.value, task.taskName);
                            if (!tasksStack.ContainsKey(task.taskId))
                                return;
                            tasksStack[task.taskId] += mess.details.ToString();
                            textBoxTaskOutput.Text = tasksStack[task.taskId];
                            textBoxTaskOutput.ScrollToEnd();
                        }
                        break;
                    }                
            });
        }

        private void Client_OnTaskFinished(object sender, PowerTask task, PowerMessage mess, bool success)
        {
            runOnUIThread(() =>
            {
                lock (listBoxRunningTasks)
                {
                    foreach (var item in listBoxRunningTasks.Items)
                        if (((TaskItem)item).taskId == task.taskId)
                        {
                            listBoxRunningTasks.Items.Remove(item);
                            break;
                        }

                    tasksStack.Remove(task.taskId);

                    if (tasksStack.Count == 0)
                        watchingTaskId = -1;
                    else
                    {
                        watchingTaskId = tasksStack.Keys.First();
                        int index = 0;
                        foreach (var item in listBoxRunningTasks.Items)
                        {
                            if (((TaskItem)item).taskId == watchingTaskId)
                                listBoxRunningTasks.SelectedIndex = index;
                            index++;
                        }
                    }
                }
            });
        }

        private void Client_OnTaskInitialized(object sender, PowerTaskFunc taskFunc, PowerTaskArgs taskArgs)
        {            
            runOnUIThread(() =>
            {
                lock (listBoxRunningTasks)
                {
                    tasksStack.Add(taskArgs.taskId, taskArgs.taskName + "\n");

                    TaskItem item = new TaskItem(taskArgs);
                    item.PreviewMouseDown += (it, arg) =>
                    {
                        if (!tasksStack.ContainsKey(item.taskId))
                            return;
                        textBoxChosenTaskToRun.Text = item.taskName;
                        watchingTaskId = item.taskId;
                        textBoxTaskOutput.Text = tasksStack[item.taskId];
                    };
                    item.Loaded += (it, e) =>
                    {
                        if (!tasksStack.ContainsKey(item.taskId))
                            listBoxRunningTasks.Items.Remove(item);
                    };

                    listBoxRunningTasks.Items.Add(item);

                    if (watchingTaskId == -1)
                        watchingTaskId = taskArgs.taskId;
                    if (listBoxRunningTasks.Items.Count == 1)
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

        private class TaskItem : Label
        {            
            public int taskId;
            public string taskName;

            public TaskItem(int taskId, string taskName)                
            {
                this.taskId = taskId;
                this.taskName = taskName;
                Content = String.Format("id({0}): {1}", taskId, taskName);
            }
            public TaskItem(PowerTask powerTask)
                : this(powerTask.taskId, powerTask.taskName)
            {

            }
        }
    }
}
