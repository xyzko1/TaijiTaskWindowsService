using Remuneration.Dal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using TaskInterface;
using WebCB.Dal;
using System.Configuration;

namespace RemunerationTask
{
    public class SynchNewMediaDataTask : TaskObject
    {
        public override string name
        {
            get
            {
                return "新媒体稿件数据同步";
            }
        }


        readonly Dictionary<string, int> fromWhere = new Dictionary<string, int>() { { "南昌日报", 1 }, { "南昌晚报", 2 }, { "南昌新闻网", 1 }, { "家庭医生报", 3 } };
        // 定义正则表达式用来匹配 img 标签
        Regex regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);
        // 定义正则表达式用来匹配 audio 标签
        Regex regAudio = new Regex(@"<audio\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<audioUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);
        // 定义正则表达式用来匹配 video 标签
        Regex regVideo = new Regex(@"<video\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<videoUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);
        public override void process()
        {

            List<EvaluateArticle> list = new List<EvaluateArticle>();
            var cbEntity = WebCBEntitiesFactory.CreateWebEntitie();
            var datas = (from pne in cbEntity.pub_newsofweibo.Where(p => !p.synch && p.commitDate.HasValue)
                         select new
                         {
                             Id = pne.id,
                             userId = pne.userid,
                             cbNewsId = pne.sourceId,
                             strAuthorId = "",
                             pne.content,
                             txtContent = pne.content,
                             title = pne.content,
                             pageview = 0,
                             evaluateCategoryId = 0,
                             articleType = 4,
                             publishDate = pne.commitDate.Value,
                             fromWhere = pne.fromwhere,
                             pne.fabulousNumber,
                             pne.forwardsNumber,
                             pne.commentsNumber,
                             pne.disseminationNumber,
                             ReprintChannelNumber = 0,
                             ReprintChannelTime = 0,
                             ReprintMediaNumber = 0,
                             ReprintNumber = 0,
                             ReadingNumber = 0
                         }).Union(
                from pn in cbEntity.pub_newsofweixin.Where(p => !p.synch && p.commitDate.HasValue && p.txtContent != null)
                select new
                {
                    Id = pn.id,
                    userId = pn.userid,
                    cbNewsId = pn.fromNewsId,
                    strAuthorId = pn.strAuthorId,
                    pn.content,
                    pn.txtContent,
                    pn.title,
                    pageview = 0,
                    evaluateCategoryId = pn.evaluateCategoryId,
                    articleType = 3,
                    publishDate = pn.commitDate.Value,
                    fromWhere = pn.strAuthor,
                    fabulousNumber = 0,
                    forwardsNumber = 0,
                    commentsNumber = 0,
                    disseminationNumber = pn.DisseminationNumber,
                    pn.ReprintChannelNumber,
                    pn.ReprintChannelTime,
                    pn.ReprintMediaNumber,
                    pn.ReprintNumber,
                    pn.ReadingNumber
                }).Union(from pne in cbEntity.pub_newsofwebsite.Where(p => !p.synch && p.commitDate.HasValue)
                         select new
                         {
                             Id = pne.id,
                             userId = pne.userid,
                             cbNewsId = pne.sourceId,
                             strAuthorId = "",
                             pne.content,
                             txtContent = pne.txtContent,
                             title = pne.title,
                             pageview = 0,
                             evaluateCategoryId = 0,
                             articleType = 1,
                             publishDate = pne.commitDate.Value,
                             fromWhere = pne.fromwhere,
                             fabulousNumber = 0,
                             forwardsNumber = 0,
                             commentsNumber = 0,
                             disseminationNumber = pne.DisseminationNumber,
                             pne.ReprintChannelNumber,
                             pne.ReprintChannelTime,
                             pne.ReprintMediaNumber,
                             pne.ReprintNumber,
                             pne.ReadingNumber
                         });

            foreach (var data in datas)
            {
                var authorIds = data.strAuthorId.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(p => int.Parse(p));
                var authors = cbEntity.v_user_info.Where(p => authorIds.Contains(p.userid) && p.typeId == 0).Select(p => new Author
                {
                    authorId = p.userid,
                    authorName = p.realname,
                    departmentId = p.departmentId ?? 0,
                    departmentName = p.departmentName
                }).ToArray();
                var kv = cbEntity.sys_dictionary.SingleOrDefault(p => p.id == data.evaluateCategoryId);
                var evaluateCategoryName = "";
                if (kv == null)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error(string.Format("稿件分类未在字典中找到值，字典Id：{0}，稿件Id：{1}", data.evaluateCategoryId, data.Id));

                }
                else
                {
                    evaluateCategoryName = kv.keyValue;
                }
                var art = new EvaluateArticle
                {
                    id = data.Id,
                    cbNewsId = data.cbNewsId,
                    txtContent = data.txtContent,
                    title = data.title,
                    pageView = data.pageview,
                    evaluateCategoryId = data.evaluateCategoryId,
                    evaluateCategoryName = evaluateCategoryName,
                    articleType = data.articleType,
                    authors = authors,
                    hasPic = regImg.Matches(data.content).Count > 0,
                    hasAudio = regAudio.Matches(data.content).Count > 0,
                    hasVedio = regVideo.Matches(data.content).Count > 0,
                    publishDate = new DateTime(data.publishDate.Year, data.publishDate.Month, data.publishDate.Day),
                    paperId = GetPaperId(data.fromWhere),
                    fromWhere = data.fromWhere,
                    disseminationNumber = data.disseminationNumber,
                    commentsNumber = data.commentsNumber,
                    fabulousNumber = data.fabulousNumber,
                    forwardsNumber = data.forwardsNumber,
                    ReadingNumber = data.ReadingNumber,
                    ReprintChannelNumber = data.ReprintChannelNumber,
                    ReprintChannelTime = data.ReprintChannelTime,
                    ReprintMediaNumber = data.ReprintMediaNumber,
                    ReprintNumber = data.ReprintNumber
                };
                art.baseScore = 0;
                list.Add(art);
            }
            cbEntity.Dispose();
            List<Tuple<int, int>> synchIds = new List<Tuple<int, int>>();

            using (RemunerationEntities entity = RemunerationEntities.Create())
            {
                foreach (var a in list)
                {
                    synchIds.Add(new Tuple<int, int>(a.id, a.articleType));
                    if (entity.art_article.Count(p => p.matchFromCB == a.id && p.publishType == a.articleType) > 0)
                        continue;
                    using (var tran = new TransactionScope())
                    {
                        //var fromType = fromWhere[a.fromWhere];
                        var fromType = 0;
                        Remuneration.Dal.cb_news news = null;
                        if (a.cbNewsId > 0)
                        {
                            news = (from n in entity.cb_news.Where(p => p.cbNewsId == a.cbNewsId && p.fromType == fromType)
                                    join art in entity.art_article.Where(p => p.picCount == 0)
                                    on n.id equals art.newsId
                                    select n).FirstOrDefault();
                        }
                        var strtitle = a.title;
                        if (strtitle.Length > 30)
                        {
                            strtitle = strtitle.Substring(0, 30);
                        }

                        if (news == null)
                        {
                            news = new Remuneration.Dal.cb_news()
                            {
                                cbNewsId = a.cbNewsId,
                                fromType = fromType,
                                title = strtitle,
                                publishDate = a.publishDate,
                                setDate = DateTime.Now,
                                hasEvaluate = false,
                                paperId = a.paperId,
                                baseScore = 0,
                                otherPlaceScale = 0,
                                isHolidy = false,
                                specialScore = 0,
                                transmissionScore = 0,
                                score = 0,
                                flowRecord = "",
                                evaluateContent = "",
                                affiliatedScore = ""
                            };
                            entity.cb_news.Add(news);
                            entity.SaveChanges();

                        }
                        else
                            news.hasEvaluate = false;
                        #region 保存art_article
                        entity.art_article.Add(new art_article
                        {
                            leadTitle = "",
                            title = strtitle,
                            subTitle = "",
                            txtContent = a.txtContent,
                            wordCount = a.txtContent.Length,
                            author = string.Join(",", a.authors.Select(p => p.authorName)),
                            authorIds = string.Join(",", a.authors.Select(p => p.authorId)),
                            departmentIds = string.Join(",", a.authors.Select(p => p.departmentId)),
                            departmentNames = string.Join(",", a.authors.Select(p => p.departmentName)),
                            picCount = 0,
                            publishDate = a.publishDate,
                            coordinate = "",
                            verOfPagePicWidth = 0,
                            verOfPagePicHeight = 0,
                            verOfPagePicPath = "",
                            picPath = "",
                            setdate = DateTime.Now,
                            paperId = a.paperId,
                            paperName = "",
                            verOfPageId = 0,
                            verOfPageName = "",
                            orderOfPageId = 0,
                            orderOfPageName = "",
                            stat = 0,
                            auditUserId = 0,
                            auditUserName = "",
                            auditDate = null,
                            categoryId = a.evaluateCategoryId,
                            categoryName = a.evaluateCategoryName,
                            editer = "",
                            setLevelUserId = 0,
                            setLevelUserName = "",
                            levelId = 0,
                            levelName = "",
                            sendType = "",
                            unit = "",
                            salary = 0,
                            remark =a.fromWhere,
                            flowRecord = "",
                            isBackToUpdate = 0,
                            patchNewsTypeId = 0,
                            baseScore = a.baseScore,
                            score = 0,
                            otherPlace = 0,
                            otherPlaceScore = 0,
                            holidy = 0,
                            holidyScore = 0,
                            other = 0,
                            otherScore = 0,
                            specialScore = 0,
                            transmissionQuantity = a.pageView,
                            transmissionScore = 0,
                            evaluateContent = "",
                            matchFromCB = a.id,
                            publishType = a.articleType,
                            hasPic = a.hasPic,
                            hasAudio = a.hasAudio,
                            hasVedio = a.hasVedio,
                            @abstract = "",
                            newsId = news.id,
                            levelScore = 0,
                            forwardsNumber = a.forwardsNumber,
                            fabulousNumber = a.fabulousNumber,
                            commentsNumber = a.commentsNumber,
                            disseminationNumber = a.disseminationNumber,
                            ReprintNumber = a.ReprintNumber,
                            ReprintMediaNumber = a.ReprintMediaNumber,
                            ReadingNumber = a.ReadingNumber,
                            ReprintChannelNumber = a.ReprintChannelNumber,
                            ReprintChannelTime = a.ReprintChannelTime
                        });
                        #endregion
                        foreach (var author in a.authors)
                        {
                            if (entity.cb_author.Count(p => p.newsId == news.id && p.authorId == author.authorId && p.typeSign == 0) > 0)
                                continue;
                            entity.cb_author.Add(new cb_author
                            {
                                newsId = news.id,
                                authorId = author.authorId,
                                typeSign = 0,
                                authorName = author.authorName,
                                coefficient = 0,
                                scoreScale = 0,
                                score = 0,
                                salary = 0,
                                ymd = int.Parse(a.publishDate.ToString("yyyyMM")),
                                paperId = a.paperId,
                                affiliatedScore = ""
                            });
                        }
                        entity.SaveChanges();
                        tran.Complete();
                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
            using (cbEntity = WebCBEntitiesFactory.CreateWebEntitie())
            {
                foreach (var data in synchIds)
                {
                    switch (data.Item2)
                    {
                        case 1:
                            {
                                var pne = cbEntity.pub_newsofwebsite.SingleOrDefault(p => p.id == data.Item1);
                                if (pne != null)
                                    pne.synch = true;
                                break;
                            }
                        case 2:
                            {
                                var pne = cbEntity.pub_newsofapp_evaluate.SingleOrDefault(p => p.Id == data.Item1);
                                if (pne != null)
                                    pne.synch = true;
                                break;
                            }
                        case 3:
                            {
                                var pne = cbEntity.pub_newsofweixin.SingleOrDefault(p => p.id == data.Item1);
                                if (pne != null)
                                    pne.synch = true;
                                break;
                            }
                        case 4:
                            {
                                var pne = cbEntity.pub_newsofweibo.SingleOrDefault(p => p.id == data.Item1);
                                if (pne != null)
                                    pne.synch = true;
                                break;
                            }
                        default:
                            break;
                    }
                }
                cbEntity.SaveChanges();
            }
            Console.WriteLine("Finish");

        }




        int GetPaperId(string paperName)
        {
            if (paperName == "南昌日报")
            {
                return 1;
            }
            else if (paperName == "南昌晚报")
            {
                return 2;
            }
            else if (paperName == "家庭医生报")
            {
                return 3;
            }
            else if (paperName == "南昌新闻网")
            {
                return 4;
            }
            else if (paperName == "南昌新闻")
            {
                return 4;
            }
            else
            {
                return 1;
            }
            //v_user_info v_user_info = cbEntity.v_user_info.SingleOrDefault(p => p.userid == userId);
            //if (v_user_info == null || !v_user_info.departmentId.HasValue)
            //    return 0;
            //var depId = cbEntity.sys_department.SingleOrDefault(p => p.departmentId == v_user_info.departmentId.Value).parentId;
            //if (string.IsNullOrEmpty(ConfigurationManager.AppSettings[depId.ToString()]))
            //    return 0;
            //return int.Parse(ConfigurationManager.AppSettings[depId.ToString()]);

        }


        int GetPaperId(WebCBEntities cbEntity, int userId)
        {
            //v_user_info v_user_info = cbEntity.v_user_info.SingleOrDefault(p => p.userid == userId);
            //if (v_user_info == null || !v_user_info.departmentId.HasValue)
            //    return 0;
            //var depId = cbEntity.sys_department.SingleOrDefault(p => p.departmentId == v_user_info.departmentId.Value).parentId;
            //if (string.IsNullOrEmpty(ConfigurationManager.AppSettings[depId.ToString()]))
            //    return 0;
            //return int.Parse(ConfigurationManager.AppSettings[depId.ToString()]);
            return 1;
        }

        class EvaluateArticle
        {
            public int id { get; set; }
            public int cbNewsId { get; set; }
            public string txtContent { get; set; }
            public string title { get; set; }
            public int pageView { get; set; }
            public int evaluateCategoryId { get; set; }
            public string evaluateCategoryName { get; set; }
            public int articleType { get; set; }
            public Author[] authors { get; set; }
            public bool hasPic { get; set; }
            public bool hasAudio { get; set; }
            public bool hasVedio { get; set; }
            public DateTime publishDate { get; set; }
            public int paperId { get; set; }
            public decimal baseScore { get; set; }
            public string fromWhere { get; set; }

            public int fabulousNumber { get; set; }
            public int forwardsNumber { get; set; }
            public int commentsNumber { get; set; }
            public decimal disseminationNumber { get; set; }
            public int ReprintChannelNumber { get; set; }
            public int ReprintChannelTime { get; set; }
            public int ReprintMediaNumber { get; set; }
            public int ReprintNumber { get; set; }
            public int ReadingNumber { get; set; }


        }

        class Author
        {
            public int authorId { get; set; }
            public string authorName { get; set; }
            public int departmentId { get; set; }
            public string departmentName { get; set; }


        }
    }
}
