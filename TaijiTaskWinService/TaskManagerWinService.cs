using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TaskInterface;

namespace TaijiTaskWinService
{
    partial class TaskManagerWinService : ServiceBase
    {
        ServiceHost host;
        public TaskManagerWinService()
        {
            InitializeComponent();
            TaskService ts = new TaskService();
            host = new ServiceHost(ts);
        }

        protected override void OnStart(string[] args)
        {
            // TODO: 在此处添加代码以启动服务。
            host.Open();
            Program.taskManager.StartAll();
        }

        protected override void OnStop()
        {
            // TODO: 在此处添加代码以执行停止服务所需的关闭操作。
            host.Close();
            Program.taskManager.StopAll();
            int retry = 100;       //等待所有任务结束或者等待超时
            while (Program.taskManager.Tasks.Count(p => p.Status != TaskInterface.TaskStatus.Stop) > 0 && retry > 0)
            {
                --retry;
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
