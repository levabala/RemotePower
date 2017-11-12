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

        public delegate void DrivesGotHandler(object sender, string[] drives);
        public event DrivesGotHandler OnDrivesGot;

        public FileSystemInfo[] serverDirectory;
        public string[] serverDrives;
        private string currentDirectory;

        public List<string> walkingHistory = new List<string>();

        public MyClient()
            : base()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var currentDirectoryParam = conf.AppSettings.Settings["CurrentDirectory"];
            if (currentDirectoryParam != null)
                if (Uri.IsWellFormedUriString(@"file:///" + currentDirectoryParam.Value.Replace("\\", "/"), UriKind.Absolute))
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
                currentDirectory = value.Replace(@"\\", @"\");                

                Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                conf.AppSettings.Settings.Remove("CurrentDirectory");
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

                    OnDrivesGot(this, serverDrives);
                    changeDirectory(CurrentDirectory);
                });
        }

        public void changeDrive(string drive)
        {
            if (!serverDrives.Contains(drive))
                return;
            string newPath = drive;
            changeDirectory(newPath);
        }

        public void changeDirectoryDeeper(string folder)
        {            
            changeDirectory(CurrentDirectory + "\\" + folder);
        }

        public void changeDirectoryUpper()
        {            
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
                            //OnDirectoryGot(this, false);
                        });
        }
    }
}