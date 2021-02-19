using Remuneration.Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskInterface;
using WebCB.Dal;

namespace RemunerationTask
{
    public class SynchUserTask : TaskObject
    {
        public override string name
        {
            get
            {
                return "采编用户同步稿费系统";
            }
        }
        int DirectorPositionId
        {
            get
            {
                return int.Parse(System.Configuration.ConfigurationManager.AppSettings["DirectorPositionId"]);
            }
        }
        public override void process()
        {
            using (RemunerationEntities entity = RemunerationEntities.Create())
            {
                var depts = entity.sys_assessmentDepartment.GroupBy(p => p.paperId).Select(p => new { paperId = p.Key, departmentIds = p.Select(d => d.departmentId) }).ToArray();
                var postions = entity.sys_user_position.ToArray();
                foreach (var dept in depts)
                {
                    var cbEntity = WebCBEntitiesFactory.CreateWebEntitie();
                    var users = (from u in cbEntity.v_user_info.Where(p => dept.departmentIds.Contains(p.departmentId.Value) && p.deleted == 0).ToArray()
                                 join p in postions
                                 on u.userid equals p.userId into Postions
                                 from p in Postions.DefaultIfEmpty()
                                 select new { user = u, positionId = p == null ? 0 : p.positionId, postionName = p == null ? "" : p.positionName }).ToArray();
                    cbEntity.Dispose();
                    int ymd = int.Parse(DateTime.Now.ToString("yyyyMM"));
                    foreach (var user in users)
                    {
                        var item = entity.rpt_authorEvaluateScoreList.SingleOrDefault(p => p.authorId == user.user.userid && p.ymd == ymd && p.paperId == dept.paperId);
                        if (item == null)
                        {
                            entity.rpt_authorEvaluateScoreList.Add(new rpt_authorEvaluateScoreList
                            {
                                authorId = user.user.userid,
                                authorName = user.user.realname,
                                departmentId = user.user.departmentId.Value,
                                departmentName = user.user.departmentName,
                                positionId = user.positionId,
                                positionName = user.postionName,
                                ymd = ymd,
                                score = 0,
                                achievementsSalary = 0,
                                circumstancesScore = 0,
                                levelId = 0,
                                levelName = "",
                                finishVerOfPageNum = 0,
                                finishVerOfPageSalary = 0,
                                finishVerOfPageScore = 0,
                                departmentTotalSalary = 0,
                                departmentSalary = 0,
                                departmentPeopleNum = 0,
                                paperId = dept.paperId,
                                assessmentSalary = 0,
                                addSalary = 0,
                                vacationSalary = 0,
                                workSalary = 0,
                                totalAmount = 0,
                                addRemark = "",
                                fixedAmount = 0,
                                noJoinAssessment = 0
                            });
                            entity.SaveChanges();
                        }
                        if (user.positionId == DirectorPositionId && entity.sys_user_noJoinAssessment.Count(p => p.userId == user.user.userid && p.ymd == ymd) == 0)
                        {
                            entity.sys_user_noJoinAssessment.Add(new sys_user_noJoinAssessment
                            {
                                userId = user.user.userid,
                                ymd = ymd
                            });
                            entity.SaveChanges();
                        }
                    }
                }


            }
        }
    }
}
