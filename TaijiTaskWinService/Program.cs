using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TaskInterface;

namespace TaijiTaskWinService
{
    class Program
    {
        public static TaskManager taskManager;
        static void Main(string[] args)
        {
            try
            {
                taskManager = TaskManager.Create();
                string taskPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var taskAssemblies = File.ReadLines(taskPath + "\\Tasks.txt").Select(p => taskPath + "\\" + p);
                var taskTypes = (from file in taskAssemblies
                                 let assembly = Assembly.LoadFile(file)
                                 from t in assembly.GetExportedTypes()
                                 where t.IsClass && typeof(ITask).IsAssignableFrom(t)
                                 select t).ToArray();
                var settings = ConfigurationManager.GetSection("taskSettings") as Dictionary<string, TaskSettings.Setting>;
                foreach (Type t in taskTypes)
                {
                    if (settings.ContainsKey(t.FullName))
                    {
                        ITask task = (ITask)Activator.CreateInstance(t);
                        TaskAgent ta = new TaskAgent(task);
                        taskManager.AddTask(ta);
                    }
                }
              //  ServiceBase.Run(new ServiceBase[] { new TaskManagerWinService() });

              //  taskManager.StartAll();

                TaskService ts = new TaskService();
                ServiceHost host = new ServiceHost(ts);
                host.Open();


                Console.ReadKey();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex);
            }
        }

    }
}
