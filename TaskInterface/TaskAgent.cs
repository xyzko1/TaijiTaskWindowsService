using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskInterface
{
    public class TaskAgent
    {
        volatile TaskStatus taskStatus;
        ITask task;
        readonly Guid id;
        readonly object lockObject = new object();
        public TaskAgent(ITask task)
        {
            this.task = task;
            taskStatus = TaskStatus.Stop;
            id = Guid.NewGuid();
        }

        internal void Start(TaskManager taskManager)
        {
            if (taskStatus != TaskStatus.Stop)
                return;
            if (task.runOnStart)
            {
                taskStatus = TaskStatus.Waitting;
                taskManager.AddProcessQueue(this, DateTime.Now);
            }
            else if (task.startTime.HasValue)
            {
                NextProcessTime = GetNextProcessTime(task.startTime.Value);
                if (NextProcessTime.HasValue)
                {
                    taskStatus = TaskStatus.Waitting;
                    taskManager.AddProcessQueue(this, NextProcessTime.Value);
                }
            }
        }

        internal void RunImmediately(TaskManager taskManager)
        {
            if (taskStatus != TaskStatus.Stop)
                return;
            taskStatus = TaskStatus.Waitting;
            taskManager.AddProcessQueue(this, DateTime.Now);
        }

        internal void Process()
        {
            try
            {
                if (taskStatus != TaskStatus.Waitting)
                    return;
                lock (lockObject)
                {
                    taskStatus = TaskStatus.Running;
                    task.process();
                    if (taskStatus == TaskStatus.WaittingForStop)
                    {
                        taskStatus = TaskStatus.Stop;
                        NextProcessTime = null;
                        return;
                    }
                    NextProcessTime = GetNextProcessTime(task.startTime ?? DateTime.Now);
                    taskStatus = NextProcessTime.HasValue ? TaskStatus.Waitting : TaskStatus.Stop;
                    NLog.LogManager.GetLogger(TaskTypeName).Info(string.Format("任务【{0}】执行完毕，状态【{1}】 下次执行时间：{2}", TaskTypeName, taskStatus.GetDescription(), NextProcessTime.HasValue ? NextProcessTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "无"));
                }
            }
            catch (Exception ex)
            {
                taskStatus = TaskStatus.Stop;
                NextProcessTime = null;
                NLog.LogManager.GetLogger(TaskTypeName).Error(ex, string.Format("TaskAgent.Process异常，任务【{0}】", TaskTypeName));
            }
        }

        internal void Stop()
        {
            if (taskStatus == TaskStatus.Running)
                taskStatus = TaskStatus.WaittingForStop;
            else if (taskStatus == TaskStatus.Waitting)
            {
                taskStatus = TaskStatus.Stop;
                NextProcessTime = null;
            }

        }

        private DateTime? GetNextProcessTime(DateTime lastTime)
        {
            switch (task.timerType)
            {
                case TimerType.Minute:
                    return GetMinuteTime(lastTime);
                case TimerType.Hour:
                    return GetHourTime(lastTime);
                case TimerType.Day:
                    return GetDayTime(lastTime);
                case TimerType.Month:
                    return GetMonthTime(lastTime);
                default:
                    if (lastTime > DateTime.Now)
                        return lastTime;
                    return null;
            }
        }

        private DateTime GetMinuteTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalMinutes / task.interval + 1;
                return time.AddMinutes(add * task.interval);
            }
            return time;
        }

        private DateTime GetHourTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalHours / task.interval + 1;
                return time.AddHours(add * task.interval);
            }
            return time;
        }

        private DateTime GetDayTime(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                var timeSpan = DateTime.Now - time;
                var add = (long)timeSpan.TotalDays / task.interval + 1;
                return time.AddDays(add * task.interval);
            }
            return time;
        }

        private DateTime GetMonthTime(DateTime time)
        {
            while (time <= DateTime.Now)
            {
                time = time.AddMonths(task.interval);
            }
            return time;
        }

        private string TaskTypeName
        {
            get
            {
                return task.GetType().FullName;
            }
        }

        public DateTime? NextProcessTime
        {
            get; private set;
        }

        public Guid Id
        {
            get { return id; }
        }

        public TaskStatus Status
        {
            get { return taskStatus; }
        }

        public string Name
        {
            get
            {
                return task.name;
            }
        }
        public int Interval
        {
            get
            {
                return task.interval;
            }
        }
        public bool RunOnStart
        {
            get
            {
                return task.runOnStart;
            }
        }
        public TimerType TaskTimerType
        {
            get
            {
                return task.timerType;
            }
        }
        public DateTime? StartTime
        {
            get
            {
                return task.startTime;
            }
        }
    }
}
