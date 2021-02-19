using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskInterface
{
    public abstract class TaskObject : ITask
    {
        public TaskObject()
        {
            var settings = ConfigurationManager.GetSection("taskSettings") as Dictionary<string, TaskSettings.Setting>;
            var setting = settings[GetType().FullName];
            interval = setting.interval;
            runOnStart = setting.runOnStart;
            startTime = setting.startTime;
            timerType = setting.timerType;
        }

        public int interval
        {
            get;
            private set;
        }
        public bool runOnStart
        {
            get;
            private set;
        }

        public DateTime? startTime
        {
            get;
            private set;
        }

        public TimerType timerType
        {
            get;
            private set;
        }

        public abstract void process();
        public abstract string name { get; }

    }
}
