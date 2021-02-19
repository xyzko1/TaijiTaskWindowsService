using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TaskInterface
{
    public class TaskSettings : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            Dictionary<string, Setting> dic = new Dictionary<string, Setting>();
            foreach (XmlNode node in section.SelectNodes("add"))
            {
                var setting = new Setting()
                {
                    interval = int.Parse(node.Attributes["interval"].Value),
                    timerType = (TimerType)int.Parse(node.Attributes["timerType"].Value),
                    startTime = null,
                    runOnStart = bool.Parse(node.Attributes["runOnStart"].Value)
                };
                DateTime time;
                if (DateTime.TryParse(node.Attributes["startTime"].Value, out time))
                    setting.startTime = time;
                dic.Add(node.Attributes["name"].Value, setting);
            }
            return dic;
        }

        public class Setting
        {
            public int interval { get; set; }
            public TimerType timerType { get; set; }
            public DateTime? startTime { get; set; }
            public bool runOnStart { get; set; }
        }
    }
}
