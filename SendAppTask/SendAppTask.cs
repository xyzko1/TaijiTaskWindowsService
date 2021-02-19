using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskInterface;
using PublishManager;
using WebCB.Dal;

namespace SendAppTask
{
    public class SendAppTask : TaskObject
    {
        public override string name => "定时APP推送";

        public override void process()
        {
            using (WebCBEntities entity = WebCBEntitiesFactory.CreateWebEntitie()) {

                NLog.LogManager.GetCurrentClassLogger().Error("sasdasdasd");
                Console.WriteLine("asdfasdf2");
            }
        }
    }
}
