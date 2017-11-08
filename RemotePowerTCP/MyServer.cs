using RemoteTCPServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTCPServerExample
{
    class MyServer : Server
    {
        new public static PowerTasksDictionary tasks = new PowerTasksDictionary();
        static string workingDirectory = @"U:\";

        public MyServer(int port)
            : base(port, registrationCallback)
        {
            
        }        

        public MyServer(int port, Dictionary<string, User> users)
            : base(port, users, registrationCallback)
        {
            
        }

        public static bool registrationCallback(string key, string ip)
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

        protected override PowerTasksDictionary CreateTasks()
        {
            PowerTasksDictionary dictionary = new PowerTasksDictionary();
            dictionary.Add(FileReadTask);
            dictionary.Add(GetFilesListTask);
            return dictionary;
        }
        
        public static PowerTaskFunc FileReadTask = new PowerTaskFunc("FileReadTask", (taskArgs, progress, result, complete, error) =>
        {
            try
            {
                string filePath = (string)taskArgs.args[0];
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                byte[] file = new byte[fs.Length];
                int chunkSize = file.Length / 10;
                int pos;
                for (pos = 0; pos < fs.Length - chunkSize; pos += chunkSize)
                {
                    fs.Read(file, pos, chunkSize);
                    progress(new PowerTaskProgress(taskArgs.taskId, taskArgs.taskName, (double)Math.Round((decimal)pos / file.Length, 2) * 100));
                }
                progress(new PowerTaskProgress(taskArgs.taskId, taskArgs.taskName, (double)Math.Round((decimal)(pos) / file.Length, 2) * 100));
                fs.Read(file, pos, file.Length - pos);
                progress(new PowerTaskProgress(taskArgs.taskId, taskArgs.taskName, 100));

                complete(new PowerTaskComplete(taskArgs.taskId, taskArgs.taskName, file));

                if (fs != null)
                    fs.Close();
            }
            catch (Exception e)
            {
                error(new PowerTaskError(taskArgs.taskId, taskArgs.taskName, e));
            }
        }, typeof(byte[]), typeof(string), "Reads a file to byte array and sends it");

        public static PowerTaskFunc GetFilesListTask = new PowerTaskFunc("GetFilesListTask", (taskArgs, progress, result, complete, error) =>
        {
            try
            {                
                string path = (string)taskArgs.args[0];
                Directory.SetCurrentDirectory(workingDirectory);
                string[] files = Directory.GetFileSystemEntries(path);
                complete(new PowerTaskComplete(taskArgs.taskId, taskArgs.taskName, files));
            }
            catch (Exception e)
            {
                error(new PowerTaskError(taskArgs.taskId, taskArgs.taskName, e));
            }
        }, typeof(string[]), typeof(string), "Generates a list of files pathes and sends it");
    }
}
