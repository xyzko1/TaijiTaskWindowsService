using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskInterface
{
    public interface ITask
    {
        void process();
        string name { get; }
        int interval { get; }
        TimerType timerType { get; }
        DateTime? startTime { get; }
        bool runOnStart { get; }
    }
}
