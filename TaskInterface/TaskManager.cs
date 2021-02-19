using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TaskInterface
{
    public class TaskManager
    {
        private static TaskManager singleton = new TaskManager();
        private readonly object lockObject = new object();    //操作queue的地方都要锁住保证线程同步
        public static TaskManager Create()
        {
            return singleton;
        }

        Dictionary<Guid, TaskAgent> tasks = new Dictionary<Guid, TaskAgent>();
        Thread thread;
        Dictionary<Guid, TaskProcess> queue;

        private TaskManager()
        {
            queue = new Dictionary<Guid, TaskProcess>();
            thread = new Thread(new ThreadStart(CheckQueue));
            thread.IsBackground = true;
            thread.Start();
        }

        void CheckQueue()
        {
            while (true)
            {
                lock (lockObject)
                {
                    List<TaskProcess> popItems = new List<TaskProcess>();
                    var time = DateTime.Now.AddSeconds(2); //增加延迟确保执行时间更加准确
                    foreach (var t in queue.Values.Where(p => p.processTime <= time).OrderBy(p => p.processTime))
                    {
                        popItems.Add(t);
                    }
                    foreach (var t in popItems)
                    {
                        queue.Remove(t.Id);
                        Process(t);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        void Process(TaskProcess tp)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(obj =>
            {
                if (tp.processTime > DateTime.Now)
                {
                    var ts = tp.processTime - DateTime.Now;
                    Thread.Sleep(ts);
                }
                var task = tp.task;
                task.Process();
                if (task.Status == TaskStatus.Waitting && task.NextProcessTime.HasValue)
                    AddProcessQueue(task, task.NextProcessTime.Value);
            }));
        }

        public bool AddTask(TaskAgent task)
        {
            if (tasks.ContainsKey(task.Id))
                return false;
            tasks.Add(task.Id, task);
            return true;
        }

        internal void AddProcessQueue(TaskAgent task, DateTime processTime)
        {
            if (task.Status != TaskStatus.Waitting)
                return;
            lock (lockObject)
            {
                if (queue.ContainsKey(task.Id))
                {
                    queue.Remove(task.Id);
                }
                var process = new TaskProcess() { task = task, processTime = processTime };
                queue.Add(process.Id, process);
            }
        }

        void ClearQueue()
        {
            lock (lockObject)
            {
                List<TaskProcess> popItems = new List<TaskProcess>();
                foreach (var t in queue.Values.Where(p => p.task.Status == TaskStatus.Stop))
                {
                    popItems.Add(t);
                }
                foreach (var t in popItems)
                {
                    queue.Remove(t.Id);
                }
            }
        }

        public void StartAll()
        {
            foreach (var t in tasks.Values.Where(p => p.Status == TaskStatus.Stop))
            {
                t.Start(this);
            }
        }
        public void StopAll()
        {
            foreach (var t in tasks.Values)
            {
                t.Stop();
            }
            ClearQueue();
        }
        public void Start(Guid id)
        {
            if (tasks.ContainsKey(id) && tasks[id].Status == TaskStatus.Stop)
            {
                tasks[id].Start(this);
            }
        }

        public void Stop(Guid id)
        {
            if (tasks.ContainsKey(id))
            {
                tasks[id].Stop();
                ClearQueue();
            }
        }

        public void RunImmediately(Guid id)
        {
            if (tasks.ContainsKey(id) && tasks[id].Status == TaskStatus.Stop)
            {
                tasks[id].RunImmediately(this);
            }
        }

        public IEnumerable<TaskAgent> Tasks
        {
            get
            {
                return tasks.Values;
            }
        }

        class TaskProcess
        {
            public TaskAgent task { get; set; }
            public DateTime processTime { get; set; }
            public Guid Id { get { return task.Id; } }
        }
    }
}
