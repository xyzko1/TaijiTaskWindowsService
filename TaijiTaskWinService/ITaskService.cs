using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace TaijiTaskWinService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“ITaskService”。
    [ServiceContract]
    public interface ITaskService
    {
        [OperationContract]
        IEnumerable<TaskResult> GetTaskList();

        [OperationContract]
        void StartAll();
        [OperationContract]
        void StopAll();
        [OperationContract]
        void Start(Guid id);
        [OperationContract]
        void Stop(Guid id);
        [OperationContract]
        void RunImmediately(Guid id);
        [OperationContract]
        DateTime Time();
    }
}
