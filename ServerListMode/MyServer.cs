using lms;
using RemoteTCPServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerListMode
{
    class MyServer : Server
    {
        new public static Dictionary<string, PowerTaskFunc> tasks = new Dictionary<string, PowerTaskFunc>();
        static string workingDirectory = @"U:\";

        public MyServer(int port)
            : base(port)
        {
            
        }

        protected override Dictionary<string, PowerTaskFunc> CreateTasks()
        {
            Dictionary<string, PowerTaskFunc> tasksDictionary = new Dictionary<string, PowerTaskFunc>();
            tasksDictionary.Add(SummatorCPUTask.taskName, SummatorCPUTask);
            return tasksDictionary;
        }
        
        public static PowerTaskFunc SummatorCPUTask = new PowerTaskFunc(
            "SummatorCPUTask",
            (taskArgs, progress, result, complete, error) =>
            {
                int taskId = taskArgs.taskId;
                string taskName = taskArgs.taskName;

                #region args processing

                #region variables
                int ref_ch0 = 0;
                int ref_chan = 31600;
                int ref_strob = 200;
                int strob = 20;
                int ref_det = 0;
                List<int> dets = new List<int>();
                double ref_phase = 0.7;
                double ref_k = 0.7128;
                double ref_r = 1 / ref_k;
                int ref_tau = 1;
                string ref_out = "low";
                int ref_frames = 50;
                List<string> names = new List<string>() { "*_raw.0??" };
                List<string> namelist = new List<string>();
                int max_det = 32;
                int max_mks = 32768;
                int maxch = max_mks / ref_tau; //32000;
                int ref_delay1 = 3200;
                bool save_all = false;
                bool save_sum = false;
                bool save_ssum = false;

                List<int[]> sp = new List<int[]>(max_det);
                double[] kt = new double[]
                {
                    1,
                    1,
                    0.9963,
                    0.9968,
                    0.9942,
                    0.9944,
                    1,
                    1,
                    0.9970,
                    0.9976,
                    0.9944,
                    0.9946,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1
                };
                #endregion

                Console.WriteLine("SummatorCPUTask ...Running...\n");

                string[] args = (string[])taskArgs.args[0];
                names.Clear();

                if (args.Length == 1) // one argument treating as single .raw or search mask
                {
                    names.Add(args[0]);
                }

                if (args.Length > 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        switch (args[i])
                        {
                            case "-raw": names.Add(args[i + 1]); break;
                            case "-det": if (int.TryParse(args[i + 1], out ref_det)) dets.Add(ref_det); break;
                            case "-mks": int.TryParse(args[i + 1], out max_mks); ref_chan = (max_mks - ref_ch0 * ref_tau) / ref_tau; break;
                            //case "-hkl": ReadHKL(args[i + 1]); break;
                            case "-tau": int.TryParse(args[i + 1], out ref_tau); break;
                            case "-delay1": int.TryParse(args[i + 1], out ref_delay1); break;
                            //case "-delay2": if (int.TryParse(args[i + 1], out ref_delay2)) Delay2Mks = ref_delay2; break;
                            case "-chan":
                                if (int.TryParse(args[i + 1], out ref_chan))
                                    max_mks = (ref_chan - ref_ch0) * ref_tau; break;
                            case "-ch0":
                                if (int.TryParse(args[i + 1], out ref_ch0))
                                    max_mks = (ref_chan - ref_ch0) * ref_tau; break;
                            case "-strob": int.TryParse(args[i + 1], out ref_strob); break;
                            case "-phase": double.TryParse(args[i + 1], out ref_phase); break;
                            case "-r": if (double.TryParse(args[i + 1], out ref_r)) ref_k = 1 / ref_r; break;
                            case "-k": if (double.TryParse(args[i + 1], out ref_k)) ref_r = 1 / ref_k; break;
                            //case "-h": LoadH(ref_hname = args[i + 1]); break;
                            //case "-cfg": config_path = args[i + 1]; break;
                            //case "-run": run = true; break;
                            //case "-pos": if (int.TryParse(args[i + 1], out ref_pos)) c.invert = (ref_pos == 1 ? true : false); break;
                            //case "-deb": c.debounce = true; break;
                            case "-w":
                                int.TryParse(args[i + 1], out ref_tau);
                                ref_chan = (max_mks - ref_ch0 * ref_tau) / ref_tau; break;
                            case "-o": ref_out = args[i + 1]; break;
                            //case "-anal": anal = true; break;
                            //case "-avg": avg = true; break;
                            //case "-rpm": write_rpm = true; break;
                            //case "-nolog": c.nolog = true; break;
                            //case "-ff": if (double.TryParse(args[i + 1], out ref_ff)) c.ff = ref_ff; break;
                            case "-frames": int.TryParse(args[i + 1], out ref_frames); break;
                            //case "-low": c.lowRes = true; break;
                            case "-dor": dets.AddRange(Enumerable.Range(0, 12)); break;
                            case "-dpr": dets.AddRange(Enumerable.Range(12, 20)); break;
                            case "-all": save_all = true; break;
                            case "-sum": save_sum = true; break;
                            case "-ssum": save_ssum = true; break;
                        }
                    }
                }

                maxch = max_mks / ref_tau;
                for (int d = 0; d < max_det; d++) sp.Add(new int[maxch]);
                strob = ref_strob / ref_tau;

                namelist = new List<string>();

                foreach (string s in names)
                {
                    if (s.Contains('*') || s.Contains('?'))
                    {
                        string[] ss = Directory.GetFiles(".", s);
                        if (ss.Length > 0) namelist.AddRange(ss);
                    }
                    else
                        if (File.Exists(s)) namelist.Add(s);
                }
                namelist.Sort();

                foreach (string fn in namelist)
                {
                    Console.WriteLine(fn);
                }

                foreach (int d in dets)
                {
                    Console.Write("{0} ", d);
                }
                Console.WriteLine();
                Console.WriteLine("w = {0} mks", ref_tau);
                Console.WriteLine("strob = {0} chan", ref_strob);
                Console.WriteLine("frames = {0}", ref_frames);
                Console.WriteLine("output = {0}", ref_out);
                
                if (!Directory.Exists(ref_out)) Directory.CreateDirectory(ref_out);
                #endregion

                object lockObj = new object();
                Summator summator = new SummatorCPU(ref_chan, max_mks, dets.ToArray(), strob);                                
                Parser.Parse(
                    namelist, strob, ref_chan, max_mks, ref_frames, ref_tau,
                    kt, dets.ToArray(), ref_ch0, (object arg, int number, double parsing, ref int savesDone) =>
                    {
                        int[][] neutrons = arg as int[][];

                        int[][] spectr = summator.CalcFrame2d(neutrons);
                        summator.SaveSpectrum(ref_out, number, spectr);
                        savesDone++;

                        lock (lockObj)
                        {
                            result(new PowerTaskResult(taskId, taskName, spectr));
                            progress(new PowerTaskProgress(taskId, taskName, parsing));
                        }
                    }, () =>
                    {
                        //parsing complete
                        complete(new PowerTaskResult(taskId, taskName, "Done"));
                    });
            }, typeof(int[][]), typeof(string[]), "ListMode summator"); 
    }
}
