using Remuneration.Dal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using TaskInterface;

namespace RemunerationTask
{
    public class HistoryDataImport : TaskObject
    {
        public override string name
        {
            get
            {
                return "考核系统历史数据导入";
            }
        }

        public override void process()
        {
            using (RemunerationEntities entity = RemunerationEntities.Create())
            {
                //Console.WriteLine("开始导入版面数据");
                //ImportPageEvaluate(entity);
                Console.WriteLine("开始导入稿件数据");
                ImportArticle(entity);
                //Console.WriteLine("开始导入特约申报数据");
                //ImportDeclareAuthor(entity);
            }
        }

        private void ImportPageEvaluate(RemunerationEntities entity)
        {
            var datas = from editor in entity.art_verOfPage_evaluate.Where(p => p.ymd == 0)
                        join page in entity.sys_paper_verOfPage
                        on editor.vid equals page.vid
                        select new { editor, page.publishDate };
            int total = datas.Count();
            int count = 0;
            foreach (var data in datas.ToArray())
            {
                data.editor.ymd = int.Parse(data.publishDate.ToString("yyyyMM"));
                data.editor.paperId = 1;
                entity.SaveChanges();
                Console.Write("\r版面数据导入...............{0}%", (++count * 100) / total);
                System.Threading.Thread.Sleep(50);
            }
            Console.WriteLine("\r版面数据转换完成");

        }

        private void ImportArticle(RemunerationEntities entity)
        {
            try
            {
                DateTime start = new DateTime(2017, 1, 1);
                DateTime end = new DateTime(2017, 12, 31);
                var query = entity.art_article.Where(p => p.newsId == 0 && p.publishDate >= start && p.publishDate < end && p.publishType == 0).OrderBy(p => p.id);
                int total = query.Count();
                for (int count = 0; count < total;)
                {
                    foreach (var a in query.Take(20).ToArray())
                    {
                        using (var tran = new TransactionScope())
                        {
                            var news = entity.cb_news.Add(new cb_news
                            {
                                cbNewsId = a.matchFromCB ?? 0,
                                fromType = a.stat == 404 ? 1 : 0,
                                title = a.title,
                                publishDate = a.publishDate,
                                setDate = a.setdate,
                                hasEvaluate = false,
                                evaluateUserId = 0,
                                evaluateUserName = "",
                                paperId = a.paperId,
                                baseScore = a.baseScore,
                                otherPlaceScale = a.otherPlaceScore,
                                isHolidy = a.holidy > 0,
                                specialScore = 0,
                                transmissionScore = 0,
                                score = a.score,
                                flowRecord = a.flowRecord,
                                evaluateContent = a.evaluateContent ?? "",
                                affiliatedScore = ""
                            });
                            entity.SaveChanges();
                            a.newsId = news.id;
                            foreach (var au in entity.art_author.Where(p => p.articleId == a.id).ToArray())
                            {
                                entity.cb_author.Add(new cb_author
                                {
                                    newsId = a.newsId,
                                    authorId = au.authorId,
                                    typeSign = au.typeSign,
                                    authorName = au.authorName,
                                    coefficient = (decimal)au.coefficient,
                                    scoreScale = au.scoreScale,
                                    score = au.score,
                                    salary = au.salary,
                                    ymd = int.Parse(a.publishDate.ToString("yyyyMM")),
                                    paperId = a.paperId,
                                    affiliatedScore = ""
                                });
                            }
                            entity.SaveChanges();
                            tran.Complete();
                            Console.Write("\r稿件数据导入...............{0}%", (++count * 100) / total);
                            System.Threading.Thread.Sleep(50);
                        }
                    }
                }
                Console.WriteLine("\r稿件数据转换完成");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void ImportDeclareAuthor(RemunerationEntities entity)
        {
            var datas = from author in entity.rpt_declareAuthor.Where(p => p.ymd == 0)
                        join declare in entity.rpt_declareArt
                        on author.articleId equals declare.id
                        join article in entity.art_article
                        on declare.sourceArticleId equals article.id
                        select new { author, article.publishDate };
            int total = datas.Count();
            int count = 0;
            foreach (var data in datas.ToArray())
            {
                data.author.ymd = int.Parse(data.publishDate.ToString("yyyyMM"));
                data.author.paperId = 1;
                entity.SaveChanges();
                Console.Write("\r特约申报数据导入...............{0}%", (++count * 100) / total);
                System.Threading.Thread.Sleep(50);
            }
            Console.WriteLine("\r特约申报数据转换完成");
        }
    }
}
