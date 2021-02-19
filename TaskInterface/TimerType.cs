using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskInterface
{
    public enum TimerType
    {
        [Description("非循环任务")]
        None,
        [Description("分钟循环任务")]
        Minute,
        [Description("小时循环任务")]
        Hour,
        [Description("天循环任务")]
        Day,
        [Description("月循环任务")]
        Month
    }
}
