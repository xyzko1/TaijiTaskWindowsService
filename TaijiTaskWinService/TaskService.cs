using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using TaskInterface;

namespace TaijiTaskWinService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“TaskService”。
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TaskService : ITaskService
    {
        public IEnumerable<TaskResult> GetTaskList()
        {
            return Program.taskManager.Tasks.Select(p => new TaskResult
            {
                id = p.Id,
                nextProcessTime = p.NextProcessTime,
                taskStatus = p.Status.GetDescription(),
                timerType = p.TaskTimerType.GetDescription(),
                interval = p.Interval,
                name = p.Name,
                runOnStart = p.RunOnStart,
                startTime = p.StartTime
            });
        }

        public void Start(Guid id)
        {
            Program.taskManager.Start(id);
        }

        public void StartAll()
        {
            Program.taskManager.StartAll();
        }

        public void Stop(Guid id)
        {
            Program.taskManager.Stop(id);
        }

        public void RunImmediately(Guid id)
        {
            Program.taskManager.RunImmediately(id);
        }

        public void StopAll()
        {
            Program.taskManager.StopAll();
        }
        public DateTime Time()
        {
            return DateTime.Now;
        }
    }
    public class TaskResult
    {
        [DataMember]
        public Guid id { get; set; }
        [DataMember]
        public DateTime? nextProcessTime { get; set; }
        [DataMember]
        public string taskStatus { get; set; }
        [DataMember]
        public string timerType { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public int interval { get; set; }
        [DataMember]
        public bool runOnStart { get; set; }
        [DataMember]
        public DateTime? startTime { get; set; }
    }
}
