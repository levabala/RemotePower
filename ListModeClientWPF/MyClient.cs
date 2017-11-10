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
        private string defaultDirectory;

        public MyClient()
            : base()
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var defaultDirectoryParam = conf.AppSettings.Settings["DefaultDirectory"];
            if (defaultDirectoryParam != null)
                defaultDirectory = defaultDirectoryParam.Value;

            OnTasksListGot += MyClient_OnTasksListGot;
        }

        public string DefaultDirectory
        {
            get
            {
                return defaultDirectory;
            }
            set
            {
                defaultDirectory = value;
                Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                conf.AppSettings.Settings.Add("DefaultDirectory", defaultDirectory);
                conf.Save();
            }            
        }

        private void MyClient_OnTasksListGot(object sender, Dictionary<string, PowerTaskFunc> dictionary)
        {
            initTask("GetDrivesTask", new object[0],
                (taskComplete) =>
                {
                    serverDrives = (string[])taskComplete.result;
                    if (DefaultDirectory == null)
                        DefaultDirectory = serverDrives[0];

                    requestDirectory(DefaultDirectory);
                });
        }

        public void requestDirectory(string path)
        {
            initTask("GetCurrentDirectoryTask", new object[] { path },
                        (taskProgress2) =>
                        {

                        },
                        (taskResult2) =>
                        {

                        },
                        (taskComplete2) =>
                        {
                            serverDirectory = (FileSystemInfo[])taskComplete2.result;
                            DefaultDirectory = path;                            
                            OnDirectoryGot(this, true);
                        },
                        (taskError2) =>
                        {
                            OnDirectoryGot(this, false);
                        });
        }
    }
}