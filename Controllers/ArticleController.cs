using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Goheer.EXIF;
using LifestyleBeta.Models;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LifestyleBeta.Controllers
{
    public class ArticleController : Controller
    {
        private LifestyleEntities db = new LifestyleEntities();

        public ActionResult Index()
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            Session["action"] = "ArticleIndex";

            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            Session["LoggedInUserId"] = UserID;

            IEnumerable<View_AllArticles> allarticles = db.View_AllArticles.Where(x => x.UserId == UserID).OrderByDescending(x => x.Updated).ToList();
            ViewBag.PublishedArticles = allarticles.Where(x => x.Status.Trim() == "P").ToList();
            ViewBag.PendingApproval = allarticles.Where(x => x.Status.Trim() == "A").ToList();
            ViewBag.Draft = allarticles.Where(x => x.Status.Trim() == "D").ToList();
            ViewBag.Rejected = allarticles.Where(x => x.Status.Trim() == "R").ToList();

            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();
            return View();
        }



        [HttpPost]
        public ActionResult AdminLogin(FormCollection col)
        {

            Session["action"] = "AdminLogin";

            string Email = Convert.ToString(col["email"]);
            string Password = Convert.ToString(col["password"]);
            if (Email == "admin" && Password == "admin" || Email == "admin2" && Password == "admin2" || Email == "admin3" && Password == "admin3")
            {
                HttpCookie spcookie = new HttpCookie("LifestyleUsers");
                spcookie["UserID"] = "0";
                spcookie["UserName"] = "Administrator-"+Email;
                spcookie["AdminUser"] = "YES";
                spcookie.Expires = DateTime.Now.AddYears(1);
                Response.Cookies.Add(spcookie);
            }

            var usr = (from u in db.tblUsers
                       where u.Email == Email && u.Password == Password
                       select new LoggedUser { FullName = u.FullName, UserId = u.UserId }
               ).ToList();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = usr,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };

        }

        public ActionResult AdminArticlesWApproval()
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.AdminUser != "YES")
            {
                return RedirectToAction("../Article/Login");
            }

            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

            return View();
        }

        public ActionResult _AdminTabPublished()
        {
            IEnumerable<View_AllArticles> allarticles = db.View_AllArticles.OrderByDescending(x => x.Updated).ToList();
            ViewBag.PublishedArticles = allarticles.Where(x => x.Status.Trim() == "P").ToList();
            return PartialView();
        }

        public ActionResult _AdminTabRejected()
        {
            IEnumerable<View_AllArticles> allarticles = db.View_AllArticles.OrderByDescending(x => x.Updated).ToList();
            ViewBag.Rejected = allarticles.Where(x => x.Status.Trim() == "R").ToList();
            return PartialView();
        }


        public ActionResult _AdminTabPending()
        {
            IEnumerable<View_AllArticles2> allarticles = db.View_AllArticles2.OrderByDescending(x => x.Updated).ToList();
            ViewBag.PendingApproval = allarticles.Where(x => x.Status.Trim() == "A").ToList();
            return PartialView();
        }

        public ActionResult _AdminTabComments()
        {
            ViewBag.Comments = db.View_CommentUserArticle.Where(x => x.Approved == 0).OrderByDescending(x => x.CommentId).ToList();
            return PartialView();
        }

        public ActionResult _AdminTabAComments()
        {
            ViewBag.AComments = db.View_CommentUserArticle.Where(x => x.Approved == 1).OrderByDescending(x => x.CommentId).ToList();
            return PartialView();
        }



        public ActionResult _AdminTabReply()
        {
            ViewBag.Reply = db.View_ReplyCommentArticleUser.Where(x => x.Approved == 0).OrderByDescending(x => x.ReplyId).ToList();
            return PartialView();
        }

        public ActionResult _AdminTabAllUsers()
        {
            ViewBag.AllUsers = db.tblUsers.Where(x => x.Status == 1).Include(c => c.tblCountry).ToList();
            return PartialView();
        }

        public ActionResult DeleteComment(int CommentId)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.AdminUser == "YES")
            {
                tblComment Tblcomment = db.tblComments.Find(CommentId);
                db.tblComments.Remove(Tblcomment);
                db.SaveChanges();
            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        public ActionResult DeleteUserComment(int CommentId)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.LoggedInUserId != 0)
            {
                int replyCount = Convert.ToInt32(db.tblCommentReplies.Where(x => x.CommentId == CommentId).ToList().Count());
                if (replyCount > 0)
                {
                    db.tblCommentReplies.RemoveRange(db.tblCommentReplies.Where(x => x.CommentId == CommentId));
                    db.SaveChanges();
                }
               

                tblComment Tblcomment = db.tblComments.Find(CommentId);
                int articleId = Convert.ToInt32(Tblcomment.ArticleId);

                db.tblComments.Remove(Tblcomment);
                db.SaveChanges();

                var tblArticle = db.tblArticles.FirstOrDefault(x => x.ArticleId == articleId);
                tblArticle.Comments = tblArticle.Comments -1;
                db.SaveChanges();


            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        public ActionResult ApproveComment(int CommentId)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.AdminUser == "YES")
            {
                tblComment Tblcomment = db.tblComments.Find(CommentId);
                Tblcomment.Approved = 1;
                db.Entry(Tblcomment).Property(x => x.Approved).IsModified = true;
                db.SaveChanges();


                var tblArticle = db.tblArticles.FirstOrDefault(x => x.ArticleId ==  Tblcomment.ArticleId);
                tblArticle.Comments = tblArticle.Comments + 1;
                db.SaveChanges();

            }



            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        public ActionResult DeleteReply(int ReplyId)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.AdminUser == "YES")
            {
                tblCommentReply TblReply = db.tblCommentReplies.Find(ReplyId);
                db.tblCommentReplies.Remove(TblReply);
                db.SaveChanges();
            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        public ActionResult ApproveReply(int ReplyId)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (ViewBag.AdminUser == "YES")
            {
                tblCommentReply TblReply = db.tblCommentReplies.Find(ReplyId);
                TblReply.Approved = 1;
                db.Entry(TblReply).Property(x => x.Approved).IsModified = true;
                db.SaveChanges();
            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        public ActionResult Login()
        {
            LoggedinOrNot();

            return View();
        }

        [HttpPost]
        public ActionResult Login(FormCollection col)
        {
            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

            Session["action"] = "Login";

            string Email = Convert.ToString(col["email"]);
            string Password = Convert.ToString(col["password"]);
            var usr = (from u in db.tblUsers
                       where u.Email == Email && u.Password == Password && u.Status > 0
                       select new LoggedUser { FullName = u.FullName, UserId = u.UserId, Status = u.Status }
                       ).ToList();

            if (usr.Count != 0)
            {
                HttpCookie spcookie = new HttpCookie("LifestyleUsers");
                spcookie["UserID"] = Convert.ToString(usr[0].UserId);
                spcookie["UserName"] = usr[0].FullName;

                if(usr[0].Status == 2)
                {
                    spcookie["AdminUser"] = "YES";
                }
                else
                {
                    spcookie["AdminUser"] = "NO";
                }
                
                spcookie.Expires = DateTime.Now.AddYears(1);
                Response.Cookies.Add(spcookie);
            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = usr,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }



        public ActionResult Logout()
        {
            Session["action"] = "Logout";

            HttpCookie spcookie = new HttpCookie("LifestyleUsers");
            spcookie["UserID"] = "";
            spcookie["UserName"] = "";
            spcookie["AdminUser"] = "";
            Response.Cookies.Add(spcookie);

            return RedirectToAction("Index", "Home");
        }

        public string LoggedinOrNot()
        {
            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

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



        public ActionResult MyAccount()
        {
            Session["action"] = "MyAccount";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            int UserId = Convert.ToInt32(ViewBag.LoggedInUserId);
            Session["LoggedInUserId"] = UserId;

            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

            ViewBag.CommentCount = db.tblComments.Where(x => x.UserId == UserId).ToList().Count();

            var MyArticles = db.View_HomeArticles.Where(x => x.UserId == UserId).OrderByDescending(x => x.ArticleId).ToList();
            ViewBag.MyArticlesCount = MyArticles.Count();
            ViewBag.MyArticles = MyArticles;

            var SavedArticles = db.View_SaveLikeArticle.Where(x => x.ProfileId == UserId && x.Type == "Save").OrderByDescending(x => x.SaveLikeId).ToList();
            ViewBag.SavedArticlesCount = SavedArticles.Count();
            ViewBag.SavedArticles = SavedArticles;

            var LikedArticles = db.View_SaveLikeArticle.Where(x => x.ProfileId == UserId && x.Type == "Like").OrderByDescending(x => x.SaveLikeId).ToList();
            ViewBag.LikedArticlesCount = LikedArticles.Count();
            ViewBag.LikedArticles = LikedArticles;

            var Likes = db.View_ArticleLikes.Where(x => x.Likes > 0 && x.UserId == UserId).ToList();
            ViewBag.LikesOnArticles = Likes;
            ViewBag.Likes = Likes.Count();
            
            ViewBag.FollowerCount = db.tblFollowers.Where(x => x.FollwerId == UserId).ToList().Count();
            ViewBag.FollowingCount = db.tblFollowers.Where(x => x.ProfileId == UserId).ToList().Count();


            List<Countrylist> cnty = (from c in db.tblCountries
                                      select new Countrylist { CountryId = c.CountryId, Name = c.Name, ISD = c.ISD }
                                 ).ToList();

            ViewBag.Countries = cnty;
            tblUser Usr = db.tblUsers.Find(UserId);

            IEnumerable<Followers> followers =
                (
                from fl in db.tblFollowers
                join j in db.tblUsers on fl.ProfileId equals j.UserId
                where fl.FollwerId == UserId
                select new Followers
                {
                    Id = fl.Id,
                    UserId = j.UserId,
                      FullName = j.FullName,
                      ProfileImage = j.ProfileImage
                     
                }
                ).ToList();

            ViewBag.Followers = followers;

            ViewBag.followedUsers = db.tblFollowers.Where(x => x.ProfileId == UserId).ToList();

            IEnumerable<Followers> following =
                (
                from fl in db.tblFollowers
                join j in db.tblUsers on fl.FollwerId equals j.UserId
                where fl.ProfileId == UserId
                select new Followers
                {
                    Id = fl.Id,
                    UserId = j.UserId,
                    FullName = j.FullName,
                    ProfileImage = j.ProfileImage

                }
                ).ToList();

            ViewBag.Following = following;

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

            return View(Usr);
        }

       
        public PartialViewResult MyProfile()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            List<tblUser> Profile = db.tblUsers.Where(x => x.UserId == UserId).ToList();

            return PartialView("_MyProfile", Profile);
        }
        public PartialViewResult MyArticles()
        {
            int UserId =Convert.ToInt32(Session["LoggedInUserId"]);
            var paraUserId = new SqlParameter("@userId", SqlDbType.Int);
            paraUserId.SqlValue = UserId;
            //List<View_AllArticles> Articles =  db.View_AllArticles.Where(x => x.UserId == UserId).OrderByDescending(x => x.ArticleId).ToList();

            List<View_AllMyArticles> Articles = db.Database.SqlQuery<View_AllMyArticles>("SP_ViewAllArticles @userId", new SqlParameter("userId", UserId))
                .OrderByDescending(x=> x.ArticleId).ToList();


            //query.ToList();

            return PartialView("_MyArticles",Articles);
        }
        public PartialViewResult MyArticleLikes()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            //var Likes = db.View_ArticleLikes.Where(x => x.Likes > 0 && x.UserId == UserId).ToList();
            //ViewBag.LikesOnArticles = Likes;
            //ViewBag.Likes = Likes.Count();

            List<View_ArticleLikes> Articles = db.View_ArticleLikes.Where(x => x.Likes > 0 && x.UserId == UserId).ToList();

            return PartialView("_MyArticleLikes", Articles);
        }

        public PartialViewResult MyComments()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);
            List<View_UserComments> Articles = db.View_UserComments.Where(x => x.UserId == UserId).OrderByDescending(x => x.CommentId).ToList();

            return PartialView("_MyComments", Articles);
        }
        public PartialViewResult MyFollowers()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            IEnumerable<Followers> followers =
               (
               from fl in db.tblFollowers
               join j in db.tblUsers on fl.ProfileId equals j.UserId
               where fl.FollwerId == UserId
               select new Followers
               {
                   Id = fl.Id,
                   UserId = j.UserId,
                   FullName = j.FullName,
                   ProfileImage = j.ProfileImage

               }
               ).ToList();

            ViewBag.Followers = followers;
            ViewBag.followedUsers = db.tblFollowers.Where(x => x.ProfileId == UserId).ToList();

            return PartialView("_MyFollowers");
        }
        public PartialViewResult MyFollowing()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            IEnumerable<Followers> following =
                 (
                 from fl in db.tblFollowers
                 join j in db.tblUsers on fl.FollwerId equals j.UserId
                 where fl.ProfileId == UserId
                 select new Followers
                 {
                     Id = fl.Id,
                     UserId = j.UserId,
                     FullName = j.FullName,
                     ProfileImage = j.ProfileImage

                 }
                 ).ToList();

            ViewBag.Following = following;

            return PartialView("_MyFollowing");
        }

        public PartialViewResult MySavedArticles()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);
            List<View_SaveLikeArticle> Articles = db.View_SaveLikeArticle.Where(x => x.ProfileId == UserId && x.Type == "Save").OrderByDescending(x => x.SaveLikeId).ToList();

            return PartialView("_MySavedArticles", Articles);
        }
        public PartialViewResult MyLikedArtciles()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);
            List<View_SaveLikeArticle> Articles = db.View_SaveLikeArticle.Where(x => x.ProfileId == UserId && x.Type == "Like").OrderByDescending(x => x.SaveLikeId).ToList();

            return PartialView("_MyLikedArticles", Articles);
        }

        public ActionResult ShowPartial()
        {
            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            List<tblUser> Profile = db.tblUsers.Where(x => x.UserId == UserId).ToList();

           // return PartialView("_MyProfile", Profile);

            return PartialView("TestPartial",Profile);
        }


        [HttpPost]
        public ActionResult FolloweBackUnFollow(int profileId, int followId,string view)
        {
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

                }
            }


            int UserId = Convert.ToInt32(Session["LoggedInUserId"]);

            IEnumerable<Followers> followers =
               (
               from fl in db.tblFollowers
               join j in db.tblUsers on fl.ProfileId equals j.UserId
               where fl.FollwerId == UserId
               select new Followers
               {
                   Id = fl.Id,
                   UserId = j.UserId,
                   FullName = j.FullName,
                   ProfileImage = j.ProfileImage

               }
               ).ToList();

            ViewBag.Followers = followers;
            ViewBag.followedUsers = db.tblFollowers.Where(x => x.ProfileId == UserId).ToList();

            return PartialView(view);
        }





        [ValidateInput(false)]
        [HttpPost]
        public ActionResult MyAccountUpdate(FormCollection col, HttpPostedFileBase file)
        {
            LoggedinOrNot();
            Session["action"] = "MyAccountUpdate";

            int UserId = Convert.ToInt32(ViewBag.LoggedInUserId);

            //string filewithpath = "";
            //string basepath = "~/Articles/" + UserId + "/ProfilePic";
            //if (file != null)
            //{

            //    string folderpath = Server.MapPath(basepath);
            //    DirectoryInfo dir = new DirectoryInfo(folderpath);
            //    if (!dir.Exists)
            //    {
            //        dir.Create();
            //    }

            //    string filx = file.FileName;
            //    filewithpath = folderpath + "\\MyPic" + Path.GetExtension(filx);
            //    file.SaveAs(filewithpath);
            //    basepath = basepath + "/MyPic" + Path.GetExtension(filx);
            //}
            //else
            //{
            //    basepath = Convert.ToString(col["filewithpath"]);
            //}

            tblUser Usr = db.tblUsers.Find(UserId);
            Usr.FullName = Convert.ToString(col["FullName"]);
            Usr.CountryId = Convert.ToInt32(col["CountryId"]);
            Usr.Gender = Convert.ToString(col["Gender"]);
            Usr.Email = Convert.ToString(col["Email"]);
            Usr.Password = Convert.ToString(col["Password"]);
            Usr.Mobile = Convert.ToString(col["Mobile"]);
            //Usr.ProfileImage = basepath;
            Usr.MyProfile = Convert.ToString(col["MyProfile"]);
            Usr.Follows = Convert.ToInt32(col["Follows"]);
            Usr.Register = Convert.ToDateTime(col["Register"]);
            Usr.Updated = DateTime.Now;
            Usr.Deleted = 0;
            Usr.Status = 1;
            db.Entry(Usr).State = EntityState.Modified;
            db.SaveChanges();


            return RedirectToAction("../Article/MyAccount");
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult UpdateProfilePicture(FormCollection col, HttpPostedFileBase file)
        {
            LoggedinOrNot();
            Session["action"] = "MyAccountUpdate";

            int UserId = Convert.ToInt32(ViewBag.LoggedInUserId);

            string filewithpath = "";
            string basepath = "~/Articles/" + UserId + "/ProfilePic";
            if (file != null)
            {

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filx = file.FileName;
                filewithpath = folderpath + "\\MyPic" + Path.GetExtension(filx);
                file.SaveAs(filewithpath);
                basepath = basepath + "/MyPic" + Path.GetExtension(filx);
            }
            else
            {
                basepath = Convert.ToString(col["filewithpath"]);
            }

            tblUser Usr = db.tblUsers.Find(UserId);
            Usr.ProfileImage = basepath;
            db.Entry(Usr).Property(x => x.ProfileImage).IsModified = true;
            db.SaveChanges();

            return RedirectToAction("../Article/MyAccount");
        }


        public ActionResult AdminAllEditors()
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            return View(db.tblUsers.Where(x => x.Status == 1).Include(c => c.tblCountry).ToList());
        }


        [HttpPost]
        public ActionResult EmailCheckerUser(FormCollection col)
        {
            string ispresent = "NO";

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("select top 1 userid from [tblUser] where email='{0}'", Convert.ToString(col["EmailID"]));
            long userx = 0;
            userx = db.Database.SqlQuery<long>(sb.ToString()).SingleOrDefault();
            if (userx != 0)
            {
                ispresent = "YES";
            }
            else
            {
                ispresent = "NO";
            }

            return new JsonpResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = ispresent,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        public ActionResult UserRegistration()
        {
            List<Countrylist> cnty = (from c in db.tblCountries
                                      select new Countrylist { CountryId = c.CountryId, Name = c.Name, ISD = c.ISD }
                                      ).ToList();
            ViewBag.Countries = cnty;
            ViewBag.CountryISD = JsonConvert.SerializeObject(cnty);
            return PartialView("UserRegistration");
        }


        [HttpPost]
        public ActionResult Register(FormCollection col)
        {
            tblUser usr = new tblUser();
            usr.FullName = Convert.ToString(col["name"]);
            usr.LastName = Convert.ToString(col["name2"]);
            usr.Email = Convert.ToString(col["EmailID"]);
            usr.Password = Convert.ToString(col["password"]);
            usr.Mobile = Convert.ToString(col["isd"]) + Convert.ToString(col["mobile"]);
            usr.Gender = Convert.ToString(col["gender"]);
            usr.CountryId = Convert.ToInt32(col["CountryId"]);
            usr.Register = DateTime.Now;
            usr.Updated = DateTime.Now;
            usr.Deleted = 0;
            usr.Status = 0;
            usr.Follows = 0;
            usr.UserGUID = Guid.NewGuid();
            db.tblUsers.Add(usr);
            db.SaveChanges();

            //            string emailbody = @"<table style='font-family:Arial'><tr><td><p style='font-size:13px;'>Hi " + usr.FullName + ",<br><br> You've successfully registered with GDNLife!</p><p style='font-size:13px;'>Click <a href='http://172.18.0.20/SOURCECODE/LifeStyle/Activate/" + usr.UserGUID + "'>here</a> to activate your account.</p></td></tr></table>";

            string path = Server.MapPath("~/Templates/LSActivate.html");
            string emailbody = System.IO.File.ReadAllText(path);
            string siteroot = ConfigurationManager.AppSettings["SiteRoot"];
            emailbody = emailbody.Replace("##activationmaillink##", siteroot + "Activate/" + usr.UserGUID + "/");
            emailbody = emailbody.Replace("##username##", usr.FullName);

            sendEmail("info@gdnlife.com", Convert.ToString(col["EmailID"]), "Activate your Account : GDNLife", emailbody);
            sendEmail("info@gdnlife.com", "binju@northstar.bh", "Activate your Account : GDNLife", emailbody);

            return new JsonpResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        public ActionResult ResendActivationMail(int userid) {

            tblUser usr = db.tblUsers.Where(x => x.UserId == userid).SingleOrDefault();

            string path = Server.MapPath("~/Templates/LSActivate.html");
            string emailbody = System.IO.File.ReadAllText(path);
            string siteroot = ConfigurationManager.AppSettings["SiteRoot"];
            emailbody = emailbody.Replace("##activationmaillink##", siteroot + "Activate/" + usr.UserGUID + "/");
            emailbody = emailbody.Replace("##username##", usr.FullName);

            sendEmail("info@gdnlife.com", Convert.ToString(usr.Email), "Activate your Account : GDNLife", emailbody);
            sendEmail("info@gdnlife.com", "binju@northstar.bh", "Activate your Account : GDNLife", emailbody);

            ViewBag.Email = usr.Email;
            return View();
        }




        [HttpPost]
        public ActionResult LostPassword(FormCollection col)
        {

            string email = Convert.ToString(col["emailost"]);

            tblUser userx = db.tblUsers.Where(x => x.Email == email).SingleOrDefault();

            if (userx.UserId != 0)
            {
                // string emailbody = @"<table style='font-family:Arial'><tr><td><p style='font-size:13px;'>Hi " + userx.FullName + ",</p><p style='font-size:13px;'>Click <a href='http://172.18.0.20/SOURCECODE/LifeStyle/PasswordReset/" + userx.UserGUID + "'>here</a> to reset your password.</p></td></tr></table>";


                string path = Server.MapPath("~/Templates/LSPassword.html");
                string emailbody = System.IO.File.ReadAllText(path);
                string siteroot = ConfigurationManager.AppSettings["SiteRoot"];
                emailbody = emailbody.Replace("##resetlink##", siteroot + "PasswordReset/" + userx.UserGUID + "/");
                emailbody = emailbody.Replace("##username##", userx.FullName);
                emailbody = emailbody.Replace("##useremail##", userx.Email);
                
                sendEmail("info@gdnlife.com", Convert.ToString(col["emailost"]), "Reset your Password : GDNLife", emailbody);
                return new JsonpResult
                {
                    ContentEncoding = Encoding.UTF8,
                    Data = "OK",
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                };
            }
            else
            {
                return new JsonpResult
                {
                    ContentEncoding = Encoding.UTF8,
                    Data = "NO",
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                };
            }


        }

        public ActionResult PasswordReset(Guid guid)
        {
            ViewBag.guid = guid;
            return View();
        }

        [HttpPost]
        public ActionResult PasswordReset(FormCollection col)
        {
            LoggedinOrNot();
            Guid guid = Guid.Parse(col["guid"]);

            tblUser userx = db.tblUsers.Where(x => x.UserGUID == guid).SingleOrDefault();
            userx.Password = Convert.ToString(col["password"]);
            db.Entry(userx).Property(x => x.Password).IsModified = true;
            db.SaveChanges();
            ViewBag.Message = "You've successfully reset your password.";
            return View();
        }


        public ActionResult Activate(Guid guid)
        {

            tblUser userx = db.tblUsers.Where(x => x.UserGUID == guid).SingleOrDefault();
            userx.Status = 1;
            db.Entry(userx).Property(x => x.Status).IsModified = true;
            db.SaveChanges();

            return View();
        }


        public ActionResult RegSuccess()
        {

            return View();
        }

   


        [HttpPost]
        public ActionResult Create(FormCollection col, HttpPostedFileBase file)
        {
            Session["action"] = "Create";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserId = Convert.ToInt32(ViewBag.LoggedInUserId);

         


            if (file != null)
            {
                tblArticle tblArticle = new tblArticle();
                tblArticle.Created = DateTime.Now;
                tblArticle.Updated = DateTime.Now;
                tblArticle.Status = "C";
                tblArticle.CategoryId = Convert.ToInt32(col["CategoryId"]);
                tblArticle.Title = Convert.ToString(col["Title"]);
                tblArticle.UserId = UserId;
                tblArticle.CoverTypeId = 1;
                tblArticle.CoverImage = "";
                tblArticle.Views = 0;
                tblArticle.Comments = 0;
                tblArticle.LeadArticle = 0;
                db.tblArticles.Add(tblArticle);
                db.SaveChanges();

                int ArticleId = Convert.ToInt32(tblArticle.ArticleId);
                int imgOrder = db.tblArticleImages.Where(x => x.ArticleID == ArticleId).ToList().Count() + 1;

                tblArticleImage addArt = new tblArticleImage();
                addArt.ArticleID = ArticleId;
                addArt.ImagePath = Convert.ToString(col["ArticleImage"]);
                addArt.Caption = Convert.ToString(col["Caption"]);
                addArt.ImageOrder = imgOrder;
                db.tblArticleImages.Add(addArt);
                db.SaveChanges();


                string basepath = "~/Articles/" + UserId + "/" + ArticleId;

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }
                string fileextn = Path.GetExtension(file.FileName);

                string filewithpath = folderpath + "/" + addArt.ArticleImageID + "_slider" + fileextn;


                //------------------------SOL2
                //if (originalImage.PropertyIdList.Contains(0x0112))
                //{
                //    int rotationValue = originalImage.GetPropertyItem(0x0112).Value[0];
                //    switch (rotationValue)
                //    {
                //        case 1: // landscape, do nothing RotateFlipType.RotateNoneFlipX
                //                // originalImage.RotateFlip(rotateFlipType: RotateFlipType.Rotate180FlipX);
                //            break;

                //        case 8: // rotated 90 right
                //                // de-rotate:
                //            originalImage.RotateFlip(rotateFlipType: RotateFlipType.Rotate270FlipNone);
                //            break;

                //        case 3: // bottoms up
                //            originalImage.RotateFlip(rotateFlipType: RotateFlipType.Rotate180FlipNone);
                //            break;

                //        case 6: // rotated 90 left
                //            originalImage.RotateFlip(rotateFlipType: RotateFlipType.Rotate90FlipNone);
                //            break;
                //    }
                //}



                ///--Original Code 
                var fileSize = file.ContentLength;
                if ((fileSize / 1048576.0) > 2) // >2mb
                {
                    System.Drawing.Image sourceimage = System.Drawing.Image.FromStream(file.InputStream);
                    System.Drawing.Image newImage = ScaleImage(sourceimage, 800);
                    newImage.Save(filewithpath, ImageFormat.Jpeg);
                }

                else
                {
                    ////originalImage
                    //System.Drawing.Image newimg2 = originalImage;
                    //newimg2.Save(filewithpath);
                    file.SaveAs(filewithpath);
                }
                ///--END Original Code 



                tblArticleImage tbx = db.tblArticleImages.Find(addArt.ArticleImageID);
                tbx.ImagePath = basepath + "/" + addArt.ArticleImageID + "_slider" + fileextn;
                db.Entry(tbx).Property(x => x.ImagePath).IsModified = true;
                db.SaveChanges();
                return RedirectToAction("../Article/Edit/" + tblArticle.ArticleId);
            }



            return RedirectToAction("../");

        }

        public PartialViewResult CoverImageRotate(int ImageId)
        {
            //int UserId = Convert.ToInt32(Session["LoggedInUserId"]);
            //int ArticleId = 20335;//Convert.ToInt32(Session["ArticleId"]);
            string ImagePath = db.tblArticleImages.Where(x => x.ArticleImageID == ImageId).Select(x => x.ImagePath).SingleOrDefault();
            //"~/Articles/" + UserId + "/" + ArticleId + "/"+ ImageId + "_slider";

           
            string sImage = ImagePath;
            //Image img = Image.FromFile(sImage);
            //img.RotateFlip(rotateFlipType: RotateFlipType.Rotate270FlipNone);
            //img.Save(ImagePath);

            ViewBag.Imagepath = ImagePath;

            return PartialView("_CoverImageRotate");
        }

        [HttpPost]
        public ActionResult CreateArticlePic(FormCollection col, HttpPostedFileBase file)
        {
            Session["action"] = "CreateArticlePic";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            int pictypeid = Convert.ToInt32(col["pictypeid"]);
            tblArticle tblArticlex = db.tblArticles.Find(ArticleId);
            tblArticlex.Caption = Convert.ToString(col["Caption"]);
            db.Entry(tblArticlex).Property(x => x.Caption).IsModified = true;

            if (file != null)
            {
                string basepath = "~/Articles/" + UserID + "/" + ArticleId;
                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filewithpath = folderpath + "/" + file.FileName;
                file.SaveAs(filewithpath);

                tblArticlex.CoverImage = basepath + "/" + file.FileName;
                db.Entry(tblArticlex).Property(x => x.CoverImage).IsModified = true;
            }

            db.SaveChanges();

            return RedirectToAction("../Article/Edit/" + ArticleId);
        }

        [HttpPost]
        public ActionResult CreateArticleVideo(FormCollection col)
        {
            Session["action"] = "CreateArticleVideo";

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
                                                                                                                                     
            string YoutubeVideoID = Convert.ToString(col["YoutubeVideoURL"]).Replace("https://www.youtube.com/watch?v=", "").Replace("https://youtu.be/", "");

            tblArticle tblArticlex = db.tblArticles.Find(ArticleId);
            tblArticlex.YoutubeVideoURL = YoutubeVideoID;
            db.SaveChanges();

            return RedirectToAction("../Article/Edit/" + ArticleId);
        }

        public ActionResult DeleteVideo(int ArticleId)
        {
            Session["action"] = "DeleteVideo";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            tblArticle tblArticle = db.tblArticles.Find(ArticleId);

            if (tblArticle.UserId == UserID) // user should only delete their own article
            {
                tblArticle.YoutubeVideoURL = "";
                db.SaveChanges();
            }

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };

        }


        public ActionResult DeleteSliderPic(int ArticleImageID)
        {
            Session["action"] = "DeleteSliderPic";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            tblArticleImage tblArticleImagex = db.tblArticleImages.Find(ArticleImageID);
            db.tblArticleImages.Remove(tblArticleImagex);
            db.SaveChanges();
            db.SaveChanges();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        public ActionResult DeletePic(int pictypeid, int ArticleId)
        {

            Session["action"] = "DeletePic";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);


            tblArticle tblArticlex = db.tblArticles.Find(ArticleId);

            if (pictypeid == 0)
            {
                tblArticlex.CoverImage = "";
                db.Entry(tblArticlex).Property(x => x.CoverImage).IsModified = true;
            }

            if (pictypeid == 1)
            {
                tblArticlex.ArticleImage1 = "";
                db.Entry(tblArticlex).Property(x => x.ArticleImage1).IsModified = true;
            }


            if (pictypeid == 2)
            {
                tblArticlex.ArticleImage2 = "";
                db.Entry(tblArticlex).Property(x => x.ArticleImage2).IsModified = true;
            }

            if (pictypeid == 3)
            {
                tblArticlex.ArticleImage3 = "";
                db.Entry(tblArticlex).Property(x => x.ArticleImage3).IsModified = true;
            }

            db.SaveChanges();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        public ActionResult DeleteArticle(int ArticleId)
        {

            Session["action"] = "DeleteArticle";
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            tblArticle tblArticle = db.tblArticles.Find(ArticleId);

            if (tblArticle.UserId == UserID || UserID == 0) // user should only delete their own article
            {

                db.tblArticleImages.RemoveRange(db.tblArticleImages.Where(x => x.ArticleID == ArticleId));
                db.SaveChanges();

                db.tblArticleSaveLikes.RemoveRange(db.tblArticleSaveLikes.Where(x => x.ArticleId == ArticleId));
                db.SaveChanges();

                db.tblComments.RemoveRange(db.tblComments.Where(x => x.ArticleId == ArticleId));
                db.SaveChanges();

                db.tblAdditionalArticles.RemoveRange(db.tblAdditionalArticles.Where(x => x.ArticleId == ArticleId));
                db.SaveChanges();

                db.tblArticles.Remove(tblArticle);
                db.SaveChanges();

                //if(Convert.ToInt32(db.tblAdditionalArticles.Where(x => x.ArticleId == ArticleId).ToList().Count()) > 0)
                //{

                //}


            }

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }




        public ActionResult DeleteSubArt(int AdditionalArticleId)
        {
            Session["action"] = "DeleteSubArt";

            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            tblAdditionalArticle tbladdart = db.tblAdditionalArticles.Find(AdditionalArticleId);
            tblArticle tblartusercheck = db.tblArticles.Find(tbladdart.ArticleId);

            if (tblartusercheck.UserId == UserID) // user should only delete their own article
            {
                
                if(tbladdart.Type == 5)
                {
                    System.IO.File.Delete(Server.MapPath(tbladdart.ArticleImage));
                }
                
                db.tblAdditionalArticles.Remove(tbladdart);
                db.SaveChanges();





            }

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        // GET: Article/Edit/5
        public ActionResult Edit(long? id)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            tblArticle tblArticle = db.tblArticles.Find(id);
            if (tblArticle.UserId != ViewBag.LoggedInUserId && ViewBag.AdminUser != "YES") // not his article
            {
                return RedirectToAction("../Article");
            }
            ViewBag.Category = db.tblCategories.Where(x => x.CategoryId != 16).ToList();

            ViewBag.SubArticles = db.tblAdditionalArticles.Where(x => x.ArticleId == id);
            ViewBag.ArticleImages = db.tblArticleImages.Where(x => x.ArticleID == id).OrderBy(x => x.ImageOrder).ToList();


            if (db.tblAdditionalArticles.Where(x => x.ArticleId == id && x.Type == 6).Count() > 0)
            {
                ViewBag.HasSlider = "YES";
            }
            else
            {
                ViewBag.HasSlider = "NO";
            }


            return View(tblArticle);
        }


        [ValidateInput(false)]
        [HttpPost]
        public ActionResult EditA(FormCollection col)
        {
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblArticle tblArticle = db.tblArticles.Find(ArticleId);
            tblArticle.Updated = DateTime.Now;
            tblArticle.Status = "A";
            tblArticle.CategoryId = Convert.ToInt32(col["CategoryId"]);
            tblArticle.Title = Convert.ToString(col["Title"]);
            tblArticle.Quote = Convert.ToString(col["Quote"]);
            tblArticle.ArticleText = Convert.ToString(col["ArticleText"]);

            EditArticle(tblArticle);

            string emailbody = @"<table style='font-family:Arial'><tr><td><p style='font-size:13px;'><br><br>An article has been submitted for approval on GDNLife!</p><p>Click to View Article<a href='http://www.gdnlife.com/Article/Edit/" + tblArticle.ArticleId + "'> " + tblArticle.Title + "</a> </p></td></tr></table>";

            sendEmail("editor@gdnonline.com", "info@gdnlife.com", "Article for Approval : GDNLife", emailbody);
            return RedirectToAction("../Article/Edit/" + ArticleId);
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult EditD(FormCollection col)
        {
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblArticle tblArticle = db.tblArticles.Find(ArticleId);
            tblArticle.Updated = DateTime.Now;
            tblArticle.Status = "D";
            tblArticle.CategoryId = Convert.ToInt32(col["CategoryId"]);
            tblArticle.Title = Convert.ToString(col["Title"]);
            tblArticle.Quote = Convert.ToString(col["Quote"]);
            tblArticle.ArticleText = Convert.ToString(col["ArticleText"]);
            EditArticle(tblArticle);
            return RedirectToAction("../Article/Edit/" + ArticleId);
        }



        public void EditArticle(tblArticle tblArticle)
        {
            db.Entry(tblArticle).Property(x => x.Updated).IsModified = true;
            db.Entry(tblArticle).Property(x => x.Status).IsModified = true;
            db.Entry(tblArticle).Property(x => x.CategoryId).IsModified = true;
            db.Entry(tblArticle).Property(x => x.Title).IsModified = true;
            db.Entry(tblArticle).Property(x => x.Quote).IsModified = true;
            db.Entry(tblArticle).Property(x => x.ArticleText).IsModified = true;
            db.SaveChanges();
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult UpdateSuper(FormCollection col)
        {
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            int CategoryId = Convert.ToInt32(col["CategoryId"]);
            string Title = Convert.ToString(col["Title"]);
            string ArticleText = Convert.ToString(col["ArticleText"]);

            tblArticle tblarticle = db.tblArticles.Find(ArticleId);
            tblarticle.CategoryId = CategoryId;
            tblarticle.Title = Title;
            tblarticle.ArticleText = ArticleText;
            db.Entry(tblarticle).Property(x => x.Title).IsModified = true;
            db.Entry(tblarticle).Property(x => x.CategoryId).IsModified = true;
            db.Entry(tblarticle).Property(x => x.ArticleText).IsModified = true;
            db.SaveChanges();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }



        [HttpPost]
        public ActionResult SuperSubTitle(FormCollection col)
        {
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            string Title = Convert.ToString(col["Title"]);
            tblAdditionalArticle tbladdarticle = db.tblAdditionalArticles.Find(AdditionalArticleId);
            tbladdarticle.Title = Title;
            db.Entry(tbladdarticle).Property(x => x.Title).IsModified = true;
            db.SaveChanges();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult SuperSubArticleText(FormCollection col)
        {
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            string ArticleText = Convert.ToString(col["ArticleText"]);
            tblAdditionalArticle tbladdarticle = db.tblAdditionalArticles.Find(AdditionalArticleId);
            tbladdarticle.ArticleText = ArticleText;
            db.Entry(tbladdarticle).Property(x => x.ArticleText).IsModified = true;
            db.SaveChanges();

            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }


        [HttpPost]
        public ActionResult Publish(int ArticleId)
        {
            tblArticle tblArticlex = db.tblArticles.Find(ArticleId);
            tblArticlex.Status = "P";
            db.Entry(tblArticlex).Property(x => x.CoverImage).IsModified = true;
            db.SaveChanges();

            long? userid = tblArticlex.UserId;
            tblUser usr = db.tblUsers.Find(userid);
            //string emailbody = @"<table style='font-family:Arial'><tr><td><p style='font-size:13px;'>Hi " + usr.FullName + ",<br><br> Your article has been successfully published in GDNLife!</p></td></tr></table>";

            string path = Server.MapPath("~/Templates/LSArticle.html");
            string emailbody = System.IO.File.ReadAllText(path);
            string siteroot = ConfigurationManager.AppSettings["SiteRoot"];

            emailbody = emailbody.Replace("##linktoarticle##", siteroot + "Home/ArticleDetail?ArticleId=" + tblArticlex.ArticleId + "&category=" + tblArticlex.CategoryId);
            emailbody = emailbody.Replace("##username##", usr.FullName);
            emailbody = emailbody.Replace("##articletitle##", tblArticlex.Title);
            
            sendEmail("info@gdnlife.com", usr.Email, "Article Published : GDNLife", emailbody);

            return RedirectToAction("../Article/AdminArticlesWApproval");
        }

        [HttpPost]
        public ActionResult Reject(FormCollection col)
        {
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblArticle tblArticlex = db.tblArticles.Find(ArticleId);
            tblArticlex.Status = "R";
            db.Entry(tblArticlex).Property(x => x.Status).IsModified = true;
            tblArticlex.Note = Convert.ToString(col["Note"]);
            db.Entry(tblArticlex).Property(x => x.Note).IsModified = true;
            db.SaveChanges();

            long? userid = tblArticlex.UserId;
            tblUser usr = db.tblUsers.Find(userid);
           // string emailbody = @"<table style='font-family:Arial'><tr><td><p style='font-size:13px;'>Hi " + usr.FullName + ",<br><br> Your article has been rejected from published in GDNLife!</p><p>Reason:" + tblArticlex.Note + "</p></td></tr></table>";

            string path = Server.MapPath("~/Templates/LSRejection.html");
            string emailbody = System.IO.File.ReadAllText(path);
            string siteroot = ConfigurationManager.AppSettings["SiteRoot"];
                        
            emailbody = emailbody.Replace("##username##", usr.FullName);
            emailbody = emailbody.Replace("##articletitle##", tblArticlex.Title);
            emailbody = emailbody.Replace("##reason##", tblArticlex.Note);


            sendEmail("info@gdnlife.com", usr.Email, "Article Rejected: GDNLife", emailbody);


            return RedirectToAction("../Article/AdminArticlesWApproval");
        }


        public ActionResult SortSliderPics(string sliderids)
        {

            string[] sliderpos = sliderids.Split(',');
            int posno = 2;
            int Imagearticleid = 0;
            foreach (var art in sliderpos)
            {
                Imagearticleid = Convert.ToInt32(art);
                tblArticleImage tblArticlex = db.tblArticleImages.Where(x => x.ArticleImageID == Imagearticleid).SingleOrDefault();
                tblArticlex.ImageOrder = posno;
                db.Entry(tblArticlex).Property(x => x.ImageOrder).IsModified = true;
                db.SaveChanges();
                posno++;
            }
            return new JsonResult
            {
                ContentEncoding = Encoding.UTF8,
                Data = "OK",
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            };
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult ArticleImage(FormCollection col, HttpPostedFileBase file)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
           

            if (file != null)
            {

                int imgOrder = db.tblArticleImages.Where(x => x.ArticleID == ArticleId).ToList().Count() + 1;

                tblArticleImage addArt = new tblArticleImage();
                addArt.ArticleID = ArticleId;
                addArt.ImagePath = Convert.ToString(col["ArticleImage"]);
                addArt.Caption = Convert.ToString(col["Caption"]);
                addArt.ImageOrder = imgOrder;
                db.tblArticleImages.Add(addArt);
                db.SaveChanges();


                string basepath = "~/Articles/" + UserID + "/" + ArticleId;

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }
                string fileextn = Path.GetExtension(file.FileName);

                string filewithpath = folderpath + "/" + addArt.ArticleImageID + "_slider" + fileextn;



                var fileSize = file.ContentLength;
                if ((fileSize / 1048576.0) > 2) // >2mb
                {
                    System.Drawing.Image sourceimage = System.Drawing.Image.FromStream(file.InputStream);

                    System.Drawing.Image newImage = ScaleImage(sourceimage, 800);
                    newImage.Save(filewithpath, ImageFormat.Jpeg);
                }

                else
                {
                    file.SaveAs(filewithpath);
                }

 

                tblArticleImage tbx = db.tblArticleImages.Find(addArt.ArticleImageID);
                tbx.ImagePath = basepath + "/" + addArt.ArticleImageID + "_slider" + fileextn;
                db.Entry(tbx).Property(x => x.ImagePath).IsModified = true;
                db.SaveChanges();

                tblAdditionalArticle addArts = db.tblAdditionalArticles.Where(x => x.ArticleId == ArticleId && x.Type == 6).SingleOrDefault();

                return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint-" + addArts.AdditionalArticleId);

            }
            return RedirectToAction("../Article/Edit/" + ArticleId);
        }


        [ValidateInput(false)]
        [HttpPost]
        public ActionResult ArticleImageUpdate(FormCollection col, HttpPostedFileBase file)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }

            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);


            int ArticleId = Convert.ToInt32(col["ArticleID"]);
            int ArticleImageID = Convert.ToInt32(col["ArticleImageID"]);


            tblArticleImage artImg = db.tblArticleImages.Find(ArticleImageID);
            artImg.Caption = Convert.ToString(col["caption"]);
            db.Entry(artImg).Property(x => x.Caption).IsModified = true;
            db.SaveChanges();

            if (file != null)
            {
                string basepath = "~/Articles/" + UserID + "/" + ArticleId;

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filewithpath = folderpath + "/" + artImg.ArticleImageID + "_slider" + Path.GetExtension(file.FileName);
                file.SaveAs(filewithpath);


                tblArticleImage tbx = db.tblArticleImages.Find(artImg.ArticleImageID);
                tbx.ImagePath = basepath + "/" + artImg.ArticleImageID + "_slider" + Path.GetExtension(file.FileName);
                db.Entry(tbx).Property(x => x.ImagePath).IsModified = true;
                db.SaveChanges();
            }
            return RedirectToAction("../Article/Edit/" + ArticleId);
        }


        [HttpPost]
        public ActionResult AdditionalArticleVideo(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            string YoutubeVideoID = Convert.ToString(col["YoutubeVideoURL"]).Replace("https://www.youtube.com/watch?v=", "").Replace("https://youtu.be/", "");
            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblAdditionalArticle addArt = new tblAdditionalArticle();
            addArt.ArticleId = ArticleId;
            addArt.YoutubeVideoURL = YoutubeVideoID;
            addArt.Type = 4;
            db.tblAdditionalArticles.Add(addArt);
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint-" + addArt.AdditionalArticleId);
        }

        [HttpPost]
        public ActionResult AdditionalArticleTitle(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblAdditionalArticle addArt = new tblAdditionalArticle();
            addArt.ArticleId = ArticleId;
            addArt.Title = Convert.ToString(col["Title"]);
            addArt.Type = 1;
            db.tblAdditionalArticles.Add(addArt);
            db.SaveChanges();
            //   return RedirectToAction("../Article/Edit/" + ArticleId + "/#addartspoint");
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint");
        }

        [ValidateInput(false)]
        [HttpPost]
        public ActionResult AdditionalArticleDetails(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblAdditionalArticle addArt = new tblAdditionalArticle();
            addArt.ArticleId = ArticleId;
            addArt.ArticleText = "<p>&nbsp;</p>";
            addArt.Type = 2;
            db.tblAdditionalArticles.Add(addArt);
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint-" + addArt.AdditionalArticleId);
        }


        [HttpPost]
        public ActionResult AddSlider(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            tblAdditionalArticle addArt = new tblAdditionalArticle();
            addArt.ArticleId = ArticleId;
            addArt.Type = 6;
            db.tblAdditionalArticles.Add(addArt);
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint");
        }


        [HttpPost]
        public ActionResult RemoveSlider(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            tblAdditionalArticle addArt = db.tblAdditionalArticles.Find(AdditionalArticleId);
            int? articleid = addArt.ArticleId;
            db.tblAdditionalArticles.Remove(addArt);
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + articleid + "#addartspoint");
        }




        [HttpPost] // images additional add
        public ActionResult AdditionalArticle(FormCollection col, HttpPostedFileBase file)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            

            if (file != null)
            {
                tblAdditionalArticle addArt = new tblAdditionalArticle();
                addArt.ArticleId = ArticleId;
                addArt.Type = 3;
                addArt.ArticleImage = Convert.ToString(col["file"]);
                addArt.Caption = Convert.ToString(col["Caption"]);
                db.tblAdditionalArticles.Add(addArt);
                db.SaveChanges();

                string basepath = "~/Articles/" + UserID + "/" + ArticleId;
                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filewithpath = folderpath + "/" + addArt.AdditionalArticleId + "_sub" + Path.GetExtension(file.FileName);
                file.SaveAs(filewithpath);


                tblAdditionalArticle tbx = db.tblAdditionalArticles.Find(addArt.AdditionalArticleId);
                tbx.ArticleImage = basepath + "/" + addArt.AdditionalArticleId + "_sub" + Path.GetExtension(file.FileName);
                db.Entry(tbx).Property(x => x.ArticleImage).IsModified = true;
                db.SaveChanges();

                return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint-"+addArt.AdditionalArticleId);
            }
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint");
        }

        [HttpPost] // images additional add
        public ActionResult AdditionalArticleVideoFile(FormCollection col, HttpPostedFileBase file)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["ArticleId"]);
            

            if (file != null)
            {
                tblAdditionalArticle addArt = new tblAdditionalArticle();
                addArt.ArticleId = ArticleId;
                addArt.Type = 5;
                addArt.ArticleImage = Convert.ToString(col["ArticleImage"]);
                addArt.Caption = Convert.ToString(col["Caption"]);
                db.tblAdditionalArticles.Add(addArt);
                db.SaveChanges();

                string basepath = "~/Articles/" + UserID + "/" + ArticleId;

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filewithpath = folderpath + "/" + addArt.AdditionalArticleId + "_video" + Path.GetExtension(file.FileName);
                file.SaveAs(filewithpath);


                tblAdditionalArticle tbx = db.tblAdditionalArticles.Find(addArt.AdditionalArticleId);
                tbx.ArticleImage = basepath + "/" + addArt.AdditionalArticleId + "_video" + Path.GetExtension(file.FileName);
                db.Entry(tbx).Property(x => x.ArticleImage).IsModified = true;
                db.SaveChanges();
                return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint-"+ tbx.AdditionalArticleId);
            }
            return Redirect(Url.Action("Edit", "Article") + "/" + ArticleId + "#addartspoint");
        }


        [HttpPost]
        public ActionResult EditAdditionalArticleTitle(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            tblAdditionalArticle addArt = db.tblAdditionalArticles.Find(AdditionalArticleId);
            addArt.Title = Convert.ToString(col["Title"]);
            db.Entry(addArt).Property(x => x.Title).IsModified = true;
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + addArt.ArticleId + "#addartspoint");
        }

        [HttpPost]
        public ActionResult EditAdditionalArticleVideo(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            string YoutubeVideoID = Convert.ToString(col["YoutubeVideoURL"]).Replace("https://www.youtube.com/watch?v=", "").Replace("https://youtu.be/", "");
            tblAdditionalArticle addArt = db.tblAdditionalArticles.Find(AdditionalArticleId);
            addArt.YoutubeVideoURL = YoutubeVideoID;
            db.Entry(addArt).Property(x => x.YoutubeVideoURL).IsModified = true;
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + addArt.ArticleId + "#addartspoint");
        }

        [HttpPost]
        public ActionResult EditAdditionalArticle(FormCollection col, HttpPostedFileBase file)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["addArticleId"]);
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);

            tblAdditionalArticle addArt = db.tblAdditionalArticles.Find(AdditionalArticleId);
            addArt.Caption = Convert.ToString(col["addCaption"]);
            db.Entry(addArt).Property(x => x.Caption).IsModified = true;
            db.SaveChanges();



            if (file != null)
            {
                string basepath = "~/Articles/" + UserID + "/" + ArticleId;

                string folderpath = Server.MapPath(basepath);
                DirectoryInfo dir = new DirectoryInfo(folderpath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                string filewithpath = folderpath + "/" + addArt.AdditionalArticleId + "_sub" + Path.GetExtension(file.FileName);
                file.SaveAs(filewithpath);


                tblAdditionalArticle tbx = db.tblAdditionalArticles.Find(addArt.AdditionalArticleId);
                tbx.ArticleImage = basepath + "/" + addArt.AdditionalArticleId + "_sub" + Path.GetExtension(file.FileName);
                db.Entry(tbx).Property(x => x.ArticleImage).IsModified = true;
                db.SaveChanges();
            }
            return Redirect(Url.Action("Edit", "Article") + "/" + addArt.ArticleId + "#addartspoint");
        }



        [ValidateInput(false)]
        [HttpPost]
        public ActionResult EditAdditionalArticleText(FormCollection col)
        {
            if (LoggedinOrNot() == "NO") { return RedirectToAction("../Article/Login"); }
            int UserID = Convert.ToInt32(ViewBag.LoggedInUserId);

            int ArticleId = Convert.ToInt32(col["addArticleId"]);
            int AdditionalArticleId = Convert.ToInt32(col["AdditionalArticleId"]);
            tblAdditionalArticle addArt = db.tblAdditionalArticles.Find(AdditionalArticleId);

            addArt.ArticleText = Convert.ToString(col["ArticleText"]);

            db.Entry(addArt).Property(x => x.ArticleText).IsModified = true;
            db.SaveChanges();
            return Redirect(Url.Action("Edit", "Article") + "/" + addArt.ArticleId + "#addartspoint");
        }

        //public void sendEmail(string from, string to, string subject, string body)
        //{
        //    MailMessage mailer = new MailMessage();
        //    mailer.From = new MailAddress(from);
        //    mailer.To.Add(new MailAddress(to));
        //    mailer.Subject = subject;
        //    mailer.Body = body;
        //    mailer.IsBodyHtml = true;
        //    SmtpClient smtp = new SmtpClient("172.18.0.20");
        //    smtp.Send(mailer);
        //}

        public void sendEmail(string from, string to, string subject, string body)
        {
            Execute(subject, body, to).Wait(50);
        }


        static async Task Execute(string subject, string body, string toadd)
        {
            var client = new SendGridClient("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");
            var from = new EmailAddress("info@gdnlife.com", "GDNLife");
            var to = new EmailAddress(toadd);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", body);
            var response = await client.SendEmailAsync(msg);
        }


        public ActionResult social(string media)
        {

            return View();
        }






        public static System.Drawing.Image ScaleImage(System.Drawing.Image image, int maxImageHeight)
        {
            /* we will resize image based on the height/width ratio by passing expected height as parameter. 
             * Based on Expected height and current image height, new ratio will be arrived and using the same we will do the resizing of image width. */

            var ratio = (double)maxImageHeight / image.Height;
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);
            var newImage = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class LoggedUser
    {
        public long UserId { get; set; }
        public string FullName { get; set; }
        public Nullable<int> Status { get; set; }
    }

    public class Followers
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string ProfileImage { get; set; }
    }

    public class Countrylist
    {
        public long CountryId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ISD { get; set; }
    }

    public class UserDetails
    {
        public int UserId { get; set; }
        public int CountryId { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string ProfileImage { get; set; }
        public int Follows { get; set; }
        public DateTime Register { get; set; }
        public DateTime Updated { get; set; }
        public int Deleted { get; set; }
        public int Status { get; set; }
        public string AdminUser { get; set; }

    }

    public class JsonpResult : JsonResult
    {
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;
            string jsoncallback = (context.RouteData.Values["jsoncallback"] as string) ?? request["jsoncallback"];
            if (!string.IsNullOrEmpty(jsoncallback))
            {
                if (string.IsNullOrEmpty(base.ContentType))
                {
                    base.ContentType = "application/x-javascript";
                }
                response.Write(string.Format("{0}((", jsoncallback));
            }
            base.ExecuteResult(context);
            if (!string.IsNullOrEmpty(jsoncallback))
            {
                response.Write("))");
            }
        }
    }
}
