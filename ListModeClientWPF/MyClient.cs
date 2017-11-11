using RemoteTCPClient;
using RemoteTCPServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListModeClientWPF
{
    class MyClient : Client
    {
        public delegate void DirectoryGotHandler(object sender, bool success);
        public event DirectoryGotHandler OnDirectoryGot;

        public FileSystemInfo[] serverDirectory;
        public string[] serverDrives;
        private string currentDirectory;

        public List<string> walkingHistory = new List<string>();

        public MyClient()
            : base()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var currentDirectoryParam = conf.AppSettings.Settings["CurrentDirectory"];
            //currentDirectoryParam = null;
            if (currentDirectoryParam != null)
                currentDirectory = currentDirectoryParam.Value;

            OnTasksListGot += MyClient_OnTasksListGot;
        }

        public string CurrentDirectory
        {
            get
            {
                return currentDirectory;
            }
            set
            {
                currentDirectory = value;                

                Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                conf.AppSettings.Settings.Add("CurrentDirectory", currentDirectory);
                conf.Save();
            }            
        }

        private void MyClient_OnTasksListGot(object sender, Dictionary<string, PowerTaskFunc> dictionary)
        {
            initTask("GetDrivesTask", new object[0],
                (taskComplete) =>
                {
                    serverDrives = (string[])taskComplete.result;
                    if (currentDirectory == null)
                        currentDirectory = serverDrives[0];

                    changeDirectory(CurrentDirectory);
                });
        }

        public void changeDirectoryDeeper(string folder)
        {
            walkingHistory.Add(folder);
            changeDirectory(CurrentDirectory + "\\" + folder);
        }

        public void changeDirectoryUpper()
        {
            walkingHistory.RemoveAt(walkingHistory.Count - 1);
            string[] dirs = CurrentDirectory.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            if (dirs.Length <= 1)
                return;
            dirs = dirs.Take(dirs.Length - 1).ToArray();
            string newPath = String.Join("\\", dirs) + "\\";            
            changeDirectory(newPath);
        }

        public void changeDirectory(string path)
        {
            initTask("GetCurrentDirectoryTask", new object[] { path },
                        (taskProgress) =>
                        {

                        },
                        (taskResult) =>
                        {

                        },
                        (taskComplete) =>
                        {                            
                            serverDirectory = (FileSystemInfo[])taskComplete.result;
                            CurrentDirectory = path;
                            OnDirectoryGot(this, true);                            
                        },
                        (taskError) =>
                        {
                            OnDirectoryGot(this, false);
                        });
        }
    }
}