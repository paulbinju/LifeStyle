using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using LifestyleBeta.Models;

namespace LifestyleBeta.Controllers
{
    public class HomeController : Controller
    {
        public LifestyleEntities db = new LifestyleEntities();

        public ActionResult Index(string p)
        {

            LoggedinOrNot();
            Session["controller"] = p;
            Session["action"] = "Index";
            
            Session["lastId"] = 0;
            //Session["pageNumber"] = 1;


            if (Request.QueryString["type"] != null && Request.QueryString["type"] != "" && Request.QueryString["type"] != null &&
                Request.QueryString["category"] != null && Request.QueryString["category"] != "" ) 
            {
                Session["type"] = Request.QueryString["type"];
                Session["category"] = Request.QueryString["category"];
                ViewBag.Title = Request.QueryString["category"];
            }
            else
            {
                Session["type"] = "";
                if (p == "" || p == "")
                {
                    p = "ALL";
                }
                Session["category"] = p;
                ViewBag.Title = p;
            }
            
            return View();
        }

        public ActionResult Test()
        {
            return View();
        }

        protected string renderPartialViewtostring(string Viewname, object model)
        {
            if (string.IsNullOrEmpty(Viewname))

                Viewname = ControllerContext.RouteData.GetRequiredString("action");
                ViewData.Model = model;

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewresult = ViewEngines.Engines.FindPartialView(ControllerContext, Viewname);
                ViewContext viewcontext = new ViewContext(ControllerContext, viewresult.View, ViewData, TempData, sw);
                viewresult.View.Render(viewcontext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }
        public class JsonModel
        {
            public string HTMLString { get; set; }
            public int NoMoredata { get; set; }

            public string RecordType { get; set; }
            public int RecordState { get; set; }

        }

        //[ChildActionOnly]
        public ActionResult table_row(List<View_HomeArticles> Model, string category,string type)
        {
            IEnumerable<View_HomeArticles> view_HomeArticles = db.View_HomeArticles.Take(1).ToList();
            IEnumerable<View_FollowersArticles> view_FollowersArticles = db.View_FollowersArticles.Take(1).ToList();
            IEnumerable<View_FollowingArticles> View_FollowingArticles = db.View_FollowingArticles.Take(1).ToList();
            Session["category"] = category;
            Session["type"] = type;
            int UserId = Convert.ToInt16(Session["UserId"]);

            
            int pageNumber = Convert.ToInt32(Session["pageNumber"]);

            if (category == "Index" || category == "" || category == null)
            {
                category = "ALL";
                Session["category"] = category;
            }
            else
            {
                Session["category"] = category;
            }

            if (Convert.ToInt32(Session["lastId"]) == 0 && category == "ALL" )
            {
                if (type == "mRead")
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Status == "P").OrderByDescending(x => x.Views).Take(20).ToList();
                    Session["lastViews"] = view_HomeArticles.Select(x => x.Views).LastOrDefault();
                    Session["pageNumber"] = 1;
                }
                else if (type == "followers")
                {
                    view_FollowersArticles = db.View_FollowersArticles.Where(x => x.FollwerId == UserId)
                           .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    var FollowerArticleId = view_FollowersArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowerArticleId.Contains(x.ArticleId));
                    Session["pageNumber"] = 1;
                }
                else if (type == "following")
                {
                       View_FollowingArticles = db.View_FollowingArticles.Where(x => x.ProfileId == UserId)
                           .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    var FollowingArticleId = View_FollowingArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowingArticleId.Contains(x.ArticleId)).OrderByDescending(x => x.ArticleId).ToList();
                    Session["pageNumber"] = 1;
                }
                else
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Status == "P").OrderByDescending(x => x.Updated).Take(20).ToList();
                    Session["pageNumber"] = 1;
                }
                
                Session["lastId"] = view_HomeArticles.Select(x => x.ArticleId).LastOrDefault();
            }
            else if(Convert.ToInt32(Session["lastId"]) == 0 && category != "ALL")
            {
                if (type == "mRead")
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Description == category && x.Status == "P").OrderByDescending(x => x.Views).Take(20).ToList();
                    Session["lastViews"] = view_HomeArticles.Select(x => x.Views).LastOrDefault();
                    Session["pageNumber"] = 1;
                }
                else if (type == "followers")
                {
                    view_FollowersArticles = db.View_FollowersArticles.Where(x => x.Description == category && x.FollwerId == UserId)
                            .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    var FollowerArticleId = view_FollowersArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowerArticleId.Contains(x.ArticleId));
                    Session["pageNumber"] = 1;
                }
                else if (type == "following")
                {
                    View_FollowingArticles = db.View_FollowingArticles.Where(x => x.Description == category && x.ProfileId == UserId)
                            .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    var FollowingArticleId = View_FollowingArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowingArticleId.Contains(x.ArticleId));
                    Session["pageNumber"] = 1;
                }
                else
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Description == category && x.Status == "P").OrderByDescending(x => x.Updated).Take(20).ToList();
                    Session["pageNumber"] = 1;
                }
                //else if (type == "followers")
                //{
                //    view_FollowersArticles = db.View_FollowersArticles.Where(x => x.CategoryName == category && x.FollwerId == UserId)
                //            .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    
                //}

                Session["lastId"] = view_HomeArticles.Select(x => x.ArticleId).LastOrDefault();
            }

            return PartialView(view_HomeArticles);
        }


        [HttpPost]
        public ActionResult InfiniteScroll()
        {
            System.Threading.Thread.Sleep(200);

            IEnumerable<View_HomeArticles> view_HomeArticles = db.View_HomeArticles.Take(1).ToList();
            IEnumerable<View_FollowersArticles> view_FollowersArticles = db.View_FollowersArticles.Take(1).ToList();
            IEnumerable<View_FollowingArticles> View_FollowingArticles = db.View_FollowingArticles.Take(1).ToList();

            string category = Convert.ToString(Session["category"]);
            string type = Convert.ToString(Session["type"]);
            int lastId = Convert.ToInt32(Session["lastId"]);

            int UserId = Convert.ToInt16(Session["UserId"]);

            int pageNumber = Convert.ToInt32(Session["pageNumber"]) + 1;

            int loadMore = 0;

            if (category == "ALL")
            {
                if (type == "mRead")
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Status == "P").OrderByDescending(x => x.Views)
                        .Skip((pageNumber - 1) * 20).Take(20).ToList();
                    
                    Session["pageNumber"] = pageNumber;
                    Session["lastId"] = view_HomeArticles.Select(x => x.ArticleId).LastOrDefault();
                    
                    lastId = Convert.ToInt32(Session["lastId"]);
                    loadMore = db.View_HomeArticles.Where(x =>  x.ArticleId < lastId).ToList().Count();
                }
                else if (type == "followers")
                {
                    view_FollowersArticles = db.View_FollowersArticles.Where(x => x.FollwerId == UserId && x.ArticleId < lastId)
                           .OrderByDescending(x => x.ArticleId).Skip((pageNumber - 1) * 20).Take(20).ToList();
                    
                    var FollowerArticleId = view_FollowersArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowerArticleId.Contains(x.ArticleId));
                    
                    Session["pageNumber"] = pageNumber;
                    Session["lastId"] = view_FollowersArticles.Select(x => x.ArticleId).LastOrDefault();
                    lastId = Convert.ToInt32(Session["lastId"]);
                    loadMore = db.View_FollowersArticles.Where(x => x.FollwerId == UserId && x.ArticleId < lastId).ToList().Count();
                }
                else if (type == "following")
                {
                    View_FollowingArticles = db.View_FollowingArticles.Where(x => x.ProfileId == UserId && x.ArticleId < lastId)
                           //.OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    .OrderByDescending(x => x.ArticleId).Skip((pageNumber - 1) * 20).Take(20).ToList();

                    var FollowingArticleId = View_FollowingArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowingArticleId.Contains(x.ArticleId));
                    
                    Session["pageNumber"] = pageNumber;
                    Session["lastId"] = View_FollowingArticles.Select(x => x.ArticleId).LastOrDefault();
                    lastId = Convert.ToInt32(Session["lastId"]);
                    loadMore = db.View_FollowingArticles.Where(x => x.ProfileId == UserId && x.ArticleId < lastId).ToList().Count();
                }
                else
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.ArticleId < lastId).OrderByDescending(x => x.Updated).Take(20).ToList();
                    Session["lastId"] = view_HomeArticles.Select(x => x.ArticleId).LastOrDefault();
                    
                    lastId = Convert.ToInt32(Session["lastId"]);
                    Session["pageNumber"] = pageNumber;
                    loadMore = db.View_HomeArticles.Where(x => x.ArticleId < lastId).ToList().Count();
                }

                

                if (loadMore > 0)
                {
                    ViewBag.loadMore = 1;
                }
                else
                {
                    ViewBag.loadMore = 0;
                }

            }
            else if (category != "ALL")
            {
                if (type == "mRead")
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Description == category && x.Status == "P").OrderByDescending(x => x.Views)
                       .Skip((pageNumber - 1) * 20).Take(20).ToList();
                    Session["pageNumber"] = pageNumber;
                }
                else if (type == "followers")
                {
                    view_FollowersArticles = db.View_FollowersArticles.Where(x => x.Description == category && x.FollwerId == UserId)
                            .OrderByDescending(x => x.ArticleId).Skip((pageNumber - 1) * 20).Take(20).ToList();
                    Session["pageNumber"] = pageNumber;

                    var FollowerArticleId = view_FollowersArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowerArticleId.Contains(x.ArticleId));
                }
                else if (type == "following")
                {
                    View_FollowingArticles = db.View_FollowingArticles.Where(x => x.Description == category && x.ProfileId == UserId)
                            .OrderByDescending(x => x.ArticleId).Take(20).ToList();
                    var FollowingArticleId = View_FollowingArticles.Select(x => x.ArticleId).ToList();
                    view_HomeArticles = db.View_HomeArticles.Where(x => FollowingArticleId.Contains(x.ArticleId));
                    Session["pageNumber"] = pageNumber;
                }
                else
                {
                    view_HomeArticles = db.View_HomeArticles.Where(x => x.Description == category && x.Status == "P"
                           && x.ArticleId < lastId).OrderByDescending(x => x.Views).Take(20).ToList();
                    Session["pageNumber"] = pageNumber;

                }

                Session["lastId"] = view_HomeArticles.Select(x => x.ArticleId).LastOrDefault();
                lastId = Convert.ToInt32(Session["lastId"]);


                loadMore = db.View_HomeArticles.Where(x => x.Description == category && x.Status == "P" && x.ArticleId < lastId).ToList().Count();

                if (loadMore > 0)
                {
                    ViewBag.loadMore = 1;
                }
                else
                {
                    ViewBag.loadMore = 0;
                }
            }

            JsonModel jsonmodel = new JsonModel();
            jsonmodel.HTMLString = renderPartialViewtostring("table_row", view_HomeArticles);
            jsonmodel.NoMoredata = Convert.ToInt32(loadMore);
            return Json(jsonmodel);
        }

        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        public ActionResult ad()
        {
            return View();
        }

        public ActionResult ArticleDetail(int ArticleId, int category = 0)
        {
            LoggedinOrNot();
            Session["action"] = "ArticleDetail";
            IEnumerable<View_ArticleDetail> ArticleDetails = db.View_ArticleDetail.Where(x => x.ArticleId == ArticleId).ToList();

            int userId = Convert.ToInt16(ArticleDetails.Select(x => x.UserId).SingleOrDefault());
            ViewBag.UserId = userId;

            if(userId !=  ViewBag.LoggedInUserId && ViewBag.AdminUser != "YES")
            {
                ArticleDetails = db.View_ArticleDetail.Where(x => x.ArticleId == ArticleId && x.Status == "P").ToList();
            }
           
            int views = Convert.ToInt16(ArticleDetails.Select(x => x.Views).SingleOrDefault()) + 1;

            tblArticle tblArticle = db.tblArticles.Find(ArticleId);
            tblArticle.Views = views;
            db.SaveChanges();

            //int[] Atypes = new int[] {3,4,5 };
            var Atypes = new List<int?> { 3, 4,5 };

            ViewBag.ArticleCovers = db.tblAdditionalArticles.Where(x => Atypes.Contains(x.Type) && x.ArticleId == ArticleId).Count();

            ViewBag.ArticleCoverImage = db.tblArticleImages.Where(x => x.ArticleID == ArticleId && x.ImageOrder == 1).Select(x => x.ImagePath).SingleOrDefault();



            ViewBag.Article = ArticleDetails;
            ViewBag.Author = ArticleDetails.Select(x => x.UserName).SingleOrDefault(); 
            ViewBag.AuthorImage = ArticleDetails.Select(x => x.ProfileImage).SingleOrDefault();

            ViewBag.AuthorProfile = ArticleDetails.Select(x => x.MyProfile).SingleOrDefault();

            if(ViewBag.AuthorProfile != null && ViewBag.AuthorProfile != "")
            {
                if (ViewBag.AuthorProfile.Length >= 150)
                {
                    ViewBag.AuthorProfile = ViewBag.AuthorProfile.Substring(0, 150);
                }
            }
            else
            {
                ViewBag.AuthorProfile = "No profile data";
            }

            int ProfileId=0;
            if (Convert.ToString(ViewBag.LoggedInUserId) !="" && Convert.ToString(ViewBag.LoggedInUserId) != null)
            {
                ProfileId = ViewBag.LoggedInUserId;
                ViewBag.Followed = db.tblFollowers.Where(x => x.ProfileId == ProfileId && x.FollwerId == userId).Count();
            }
            else
            {
                ViewBag.Followed = 0;
            }



            ViewBag.Saved = db.tblArticleSaveLikes.Where(x => x.ProfileId == ProfileId && x.ArticleId == ArticleId && x.Type == "Save").Count();
            ViewBag.Liked = db.tblArticleSaveLikes.Where(x => x.ProfileId == ProfileId && x.ArticleId == ArticleId && x.Type == "Like").Count();

            ViewBag.PostCount = db.View_ArticleDetail.Where(x => x.UserId == userId && x.Status == "P").Count();

            var ArticleComments = db.View_ArticleComment.Where(x => x.ArticleId == ArticleId && x.Approved == 1).ToList().OrderByDescending(x => x.CommentId);
            ViewBag.ArticleComments = ArticleComments;
            ViewBag.TotalComment = ArticleComments.Count();
            
            var CommentsReply = db.View_CommentReply.Where(x => x.ArticleId == ArticleId && x.Approved == 1).ToList().OrderBy(x => x.ReplyId);
            ViewBag.CommentsReply = CommentsReply;

            IEnumerable<AuthorCategory> AuthorCategory = (
                from ad in db.View_ArticleDetail
                where ad.UserId == userId && ad.Status == "P"
                group ad by new { ad.CategoryId, ad.CategoryName, ad.CategoryClass } into g
                select new AuthorCategory
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    CategoryClass = g.Key.CategoryClass
                }

                ).ToList();

            ViewBag.AuthorCategory = AuthorCategory;


            ViewBag.AutohrFollowers = db.View_Followers.Where(x => x.FollwerId == userId).ToList();
            ViewBag.AutohrFollowing = db.View_Following.Where(x => x.ProfileId == userId).ToList();

            ViewBag.FollowerCount = db.tblFollowers.Where(x => x.FollwerId == userId).ToList().Count();
            ViewBag.FollowingCount = db.tblFollowers.Where(x => x.ProfileId == userId).ToList().Count();

            var AuthorArticles = db.View_AllArticles.Where(x => x.UserId == userId && x.Status == "P").OrderByDescending(x => x.ArticleId).Take(20).ToList();
            ViewBag.AuthorArticles = AuthorArticles;

            ViewBag.LikeArticles = db.View_AllArticles.Where(x => x.CategoryId == category && x.Status == "P").OrderByDescending(x => x.ArticleId).Take(20).ToList();

            ViewBag.SliderImages = db.tblArticleImages.Where(x => x.ArticleID == ArticleId && x.ImageOrder > 1).OrderBy(x => x.ImageOrder).ToList();

            ViewBag.AdditionalArticles = db.tblAdditionalArticles.Where(x => x.ArticleId == ArticleId).ToList();


            return View();
        }

       
        
        public ActionResult Author(int UserId = 0)
        {
            LoggedinOrNot();
            Session["action"] = "Author";

            ViewBag.UserId = UserId;

            var AtuhorArticles = db.View_AllArticles.Where(x => x.UserId == UserId && x.Status == "P").OrderByDescending(x => x.Created).ToList();
            ViewBag.AtuhorArticles = AtuhorArticles;

            ViewBag.TopArticles = AtuhorArticles.OrderBy(x => x.ArticleId).Take(4);
            ViewBag.LatestArticles = AtuhorArticles;

            ViewBag.Author = db.tblUsers.FirstOrDefault(u => u.UserId == UserId).FullName;
            ViewBag.Image = db.tblUsers.FirstOrDefault(u => u.UserId == UserId).ProfileImage;
            ViewBag.Profile = db.tblUsers.FirstOrDefault(u => u.UserId == UserId).MyProfile;

            

            int ProfileId = 0;
            if (Convert.ToString(ViewBag.LoggedInUserId) != "" && Convert.ToString(ViewBag.LoggedInUserId) != null)
            {
                ProfileId = ViewBag.LoggedInUserId;
                ViewBag.Followed = db.tblFollowers.Where(x => x.ProfileId == ProfileId && x.FollwerId == UserId).Count();
            }
            else
            {
                ViewBag.Followed = 0;
            }
            
            ViewBag.PostCount = db.View_ArticleDetail.Where(x => x.UserId == UserId && x.Status == "P").Count();

            ViewBag.AuthorDetails = db.tblUsers.Where(x => x.UserId == UserId).ToList();

            IEnumerable<AuthorCategory> AuthorCategory = (
              from ad in db.View_ArticleDetail
              where ad.UserId == UserId && ad.Status == "P"
              group ad by new { ad.CategoryId, ad.CategoryName, ad.CategoryClass } into g
              select new AuthorCategory
              {
                  CategoryId = g.Key.CategoryId,
                  CategoryName = g.Key.CategoryName,
                  CategoryClass = g.Key.CategoryClass
              }

              ).ToList();


            ViewBag.AuthorCategory = AuthorCategory;

            ViewBag.FollowerCount = db.tblFollowers.Where(x => x.FollwerId == UserId).ToList().Count();
            ViewBag.FollowingCount = db.tblFollowers.Where(x => x.ProfileId == UserId).ToList().Count();



            return View();
        }

        public ActionResult SearchList(FormCollection fc)
        {

            LoggedinOrNot();
            Session["action"] = "SearchList";

            string SearchText = fc["SearchText"];
            ViewBag.SearchText = SearchText;

            var SearchArticles = db.View_ArticleDetail.Where(x => x.Title.ToUpper().Contains(SearchText.ToUpper()) || x.ArticleText.Contains(SearchText) && x.Status == "P");
            ViewBag.SearchArticles = SearchArticles;
            //ViewBag.LatestArticles = SearchArticles;

            return View();
        }

        public ActionResult Page(string pageName)
        {
            //Session["Controller"] = "Home";
            //Session["Action"] = "Category";
            ViewBag.HomeArticles = db.View_HomeArticles.Where(x => x.Description == pageName && x.Status == "P").ToList().OrderByDescending(x => x.ArticleId);

            
            ViewBag.Title = pageName;
            ViewBag.ActivePage = pageName;

            return View();
        }


        [HttpPost]
        public ActionResult SaveLikeArticle(string type, int articleId,int profileId)
        {
            JsonModel jsonmodel = new JsonModel();
            var tblArticleSL = new tblArticleSaveLike();

            if (type != "" && type != null && articleId != 0 && Convert.ToString(articleId) != "" && profileId != 0 && Convert.ToString(profileId) != "")
            {
                var existsRecord = db.tblArticleSaveLikes.Where(x => x.Type == type && x.ArticleId == articleId && x.ProfileId == profileId).FirstOrDefault();

                if (existsRecord != null)
                {
                    int existsId = Convert.ToInt32(existsRecord.Id);

                    var deleteRecord = db.tblArticleSaveLikes.Find(existsId);
                    db.tblArticleSaveLikes.Remove(deleteRecord);
                    db.SaveChanges();

                    if (type == "Like")
                    {
                        var tblArticle = db.tblArticles.FirstOrDefault(x => x.ArticleId == articleId);
                        tblArticle.Likes = tblArticle.Likes - 1;
                        db.SaveChanges();
                    }

                    jsonmodel.RecordState = 1;
                    jsonmodel.RecordType = type;
                }
                else
                {
                    tblArticleSL = new tblArticleSaveLike()
                    {
                        ArticleId = articleId,
                        ProfileId = profileId,
                        Type = type
                    };
                    db.tblArticleSaveLikes.Add(tblArticleSL);
                    db.SaveChanges();

                    if (type == "Like")
                    {
                        var tblArticle = db.tblArticles.FirstOrDefault(x => x.ArticleId == articleId);

                        if (tblArticle.Likes ==null)
                        {
                            tblArticle.Likes = 0;
                        }
                        tblArticle.Likes = tblArticle.Likes + 1;
                        db.SaveChanges();
                    }

                    jsonmodel.RecordState = 2;
                    jsonmodel.RecordType = type;
                }
            }
            else
            {
                jsonmodel.HTMLString = "Good Night";
                //return Json("error", JsonRequestBehavior.AllowGet);
            }

            return Json(jsonmodel);
        }


        public ActionResult DeleteSavedArticle(int savelikeId)
        {

            Session["action"] = "DeleteSavedArticle";
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            tblArticleSaveLike tblArticleSaveLike = db.tblArticleSaveLikes.Find(savelikeId);

            if (tblArticleSaveLike.ProfileId == UserID) // user should only delete their own article
            {
                db.tblArticleSaveLikes.Remove(tblArticleSaveLike);
                db.SaveChanges();
            }

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        [HttpPost]
        public ActionResult Followers(int profileId, int followId)
        {
            JsonModel jsonmodel = new JsonModel();
            var tblFollow = new tblFollower();

            if (followId != 0 && Convert.ToString(followId) != "" && profileId != 0 && Convert.ToString(profileId) != "")
            {
                var existsRecord = db.tblFollowers.Where(x => x.FollwerId == followId && x.ProfileId == profileId).FirstOrDefault();

                if (existsRecord != null)
                {
                    int existsId = Convert.ToInt32(existsRecord.Id);

                    var deleteRecord = db.tblFollowers.Find(existsId);
                    db.tblFollowers.Remove(deleteRecord);
                    db.SaveChanges();

                    jsonmodel.RecordState = 1;
                    jsonmodel.RecordType = "Follow";
                }
                else
                {
                    tblFollow = new tblFollower()
                    {
                        FollwerId = followId,
                        ProfileId = profileId
                    };
                    db.tblFollowers.Add(tblFollow);
                    db.SaveChanges();

                    jsonmodel.RecordState = 2;
                    jsonmodel.RecordType = "UnFollow";
                }
            }
            else
            {
                jsonmodel.HTMLString = "Good Night";
                //return Json("error", JsonRequestBehavior.AllowGet);
            }

            return Json(jsonmodel);
        }

        



        [HttpPost]
        public ActionResult ArticleComment(int articleId,int profileId, string comment)
        {
            JsonModel jsonmodel = new JsonModel();
            var tbcommnet = new tblComment();

            tbcommnet = new tblComment()
            {
                ArticleId = articleId,
                UserId = profileId,
                CommentText = comment,
                Created = DateTime.Now,
                Deleted=0,
                Approved=0
            };

            db.tblComments.Add(tbcommnet);
            db.SaveChanges();

            //var tblArticle = db.tblArticles.FirstOrDefault(x => x.ArticleId == articleId);
            //tblArticle.Comments = tblArticle.Comments + 1;
            //db.SaveChanges();


            jsonmodel.RecordState = 1;
            //return View (jsonmodel);
            return Json(jsonmodel);
        }


        [HttpPost]
        public ActionResult CommentReply(FormCollection col)
        {

            LoggedinOrNot();

            tblCommentReply tblcr = new tblCommentReply()
            {
                CommentId = Convert.ToInt32(col["CommentId"]),
                Reply = Convert.ToString(col["Reply"]),
                UserId = Convert.ToInt32(ViewBag.LoggedInUserId),
                Created = DateTime.Now,
                Approved = 0
                
            };
            db.tblCommentReplies.Add(tblcr);
            db.SaveChanges();

            return Json("OK");
        }

        public string LoggedinOrNot()
        {
            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

            ViewBag.ArticleWACount = db.tblArticles.Where(x => x.Status == "A").Count();

            if (Request.Cookies["LifestyleUsers"] != null)
            {
                if (Convert.ToString(Request.Cookies["LifestyleUsers"]["UserID"]) != "")
                {
                    ViewBag.LoggedInUserId = Convert.ToInt32(Request.Cookies["LifestyleUsers"]["UserID"]);
                    Session["UserId"] = ViewBag.LoggedInUserId;
                    ViewBag.UserName = Convert.ToString(Request.Cookies["LifestyleUsers"]["UserName"]);
                    ViewBag.AdminUser = Convert.ToString(Request.Cookies["LifestyleUsers"]["AdminUser"]);
                    return "YES";
                }
                else
                {
                    ViewBag.LoggedInUserId = null;
                    return "NO";
                }
            }
            else
            {
                ViewBag.LoggedInUserId = null;
                return "NO";
            }

        }



        public ActionResult About()
        {
            Session["Controller"] = "Home";
            Session["Action"] = "About";
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            Session["Controller"] = "Home";
            Session["Action"] = "Contact";

            ViewBag.Message = "Your contact page.";

            return View();
        }
    }

    public class AuthorCategory
    {
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryClass { get; set; }
    }
}