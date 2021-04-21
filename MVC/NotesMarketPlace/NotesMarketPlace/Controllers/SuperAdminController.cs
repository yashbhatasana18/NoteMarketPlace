using System.Linq;
using System.Web.Mvc;
using NotesMarketPlace.DB;
using NotesMarketPlace.Models.Admin;
using System.Web.Routing;
using System.Web.Security;
using System;
using System.Collections.Generic;
using NotesMarketPlace.Models;
using System.IO;

namespace NotesMarketPlace.Controllers
{
    [Authorize(Roles = "Super Admin")]
    public class SuperAdminController : Controller
    {
        #region Default Constructor

        public SuperAdminController()
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var socialUrl = context.SystemConfigurations.Where(m => m.Key == "Facebook" || m.Key == "Twitter" || m.Key == "Linkedin").ToList();
                ViewBag.URLs = socialUrl;
            }
        }

        #endregion Default Constructor

        #region Initialize User Information

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (requestContext.HttpContext.User.Identity.IsAuthenticated)
            {
                using (var context = new NotesMarketPlaceEntities())
                {
                    var currentUser = context.Users.FirstOrDefault(m => m.EmailID == User.Identity.Name);

                    var img = (from Details in context.UserProfile
                               join Users in context.Users on Details.UserID equals Users.UserID
                               where Users.EmailID == requestContext.HttpContext.User.Identity.Name
                               select Details.ProfilePicture).FirstOrDefault();

                    string fileName = System.IO.Path.GetFileName(img);

                    string filePath = "Members/" + currentUser.UserID + "/" + fileName;

                    if (img == null)
                    {
                        var defaultImg = context.SystemConfigurations.FirstOrDefault(m => m.Key == "DefaultProfileImage").Value;
                        ViewBag.UserProfile = defaultImg;
                    }
                    else
                    {
                        ViewBag.UserProfile = filePath;
                    }
                }
            }
        }

        #endregion Initialize User Information

        #region Manage Administrator

        public ActionResult ManageAdministrator(string txtSearch, string SortOrder, string SortBy, int PageNumber = 1)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var model = (from User in context.Users
                             where User.RoleID == 2
                             join Profile in context.UserProfile on User.UserID equals Profile.UserID
                             select new ManageAdministratorModel
                             {
                                 Id = User.UserID,
                                 FirstName = User.FirstName,
                                 LastName = User.LastName,
                                 Email = User.EmailID,
                                 Phone = Profile.PhoneNumber,
                                 Active = User.IsActive == true ? "Yes" : "No",
                                 DateAdded = User.CreatedDate
                             }).OrderByDescending(x => x.DateAdded).ToList();

                ViewBag.SortOrder = SortOrder;
                ViewBag.SortBy = SortBy;
                var ManageAdministratorresult = model;

                if (txtSearch != null)
                {
                    if (ManageAdministratorresult.Any(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())))
                    {
                        ManageAdministratorresult = ManageAdministratorresult.Where(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else if (ManageAdministratorresult.Any(x => x.LastName.ToLower().Contains(txtSearch.ToLower())))
                    {
                        ManageAdministratorresult = ManageAdministratorresult.Where(x => x.LastName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else if (ManageAdministratorresult.Any(x => x.Email.ToLower().Contains(txtSearch.ToLower())))
                    {
                        ManageAdministratorresult = ManageAdministratorresult.Where(x => x.Email.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }
                    else
                    {
                        ManageAdministratorresult = ManageAdministratorresult.Where(x => x.FirstName.ToLower().Contains(txtSearch.ToLower())).ToList();
                    }

                    //Sorting
                    ManageAdministratorresult = ApplySorting(SortOrder, SortBy, ManageAdministratorresult);

                    //Pagination
                    ManageAdministratorresult = ApplyPagination(ManageAdministratorresult, PageNumber);
                }
                else
                {
                    //Sorting
                    ManageAdministratorresult = ApplySorting(SortOrder, SortBy, ManageAdministratorresult);

                    //Pagination
                    ManageAdministratorresult = ApplyPagination(ManageAdministratorresult, PageNumber);
                }

                return View(ManageAdministratorresult);
            }
        }

        #endregion Manage Administrator

        #region Add Administrator

        public ActionResult AddAdministrator(int? id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                //Get Country List
                var country = context.Countries.ToList();
                var CountryCodeList = country;

                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                if (!id.Equals(null))
                {
                    var model = (from User in context.Users
                                 where User.UserID == id && User.IsActive == true
                                 join Detail in context.UserProfile on User.UserID equals Detail.UserID
                                 select new AddAdministratorModel
                                 {
                                     Id = User.UserID,
                                     FirstName = User.FirstName,
                                     LastName = User.LastName,
                                     Email = User.EmailID,
                                     CountryCode = Detail.PhoneNumberCountryCode,
                                     Phone = Detail.PhoneNumber
                                 }).Single();

                    var countryCode = context.Countries.Where(m => m.IsActive == true).ToList();

                    ViewBag.PhoneCode = countryCode;
                    ViewBag.Edit = true;

                    return View(model);
                }
                else
                {
                    var countryCode = context.Countries.Where(m => m.IsActive == true).ToList();
                    ViewBag.PhoneCode = countryCode;
                    AddAdministratorModel model = new AddAdministratorModel();

                    ViewBag.Edit = false;
                    return View();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAdministrator(int? id, AddAdministratorModel model)
        {
            if (!ModelState.IsValid)
            {
                if (id.Equals(null))
                {
                    return RedirectToAction("AddAdministrator");
                }
                else
                {
                    return RedirectToAction("AddAdministrator", id);
                }
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var CurrentUser = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;
                var country = context.Countries.ToList();
                var CountryCodeList = country;

                ViewBag.CountryCodeList = new SelectList(CountryCodeList, "CountriesID", "CountryCode");

                //For Edit Details
                if (!id.Equals(null))
                {
                    var data = context.Users.Single(m => m.UserID == id);
                    var details = context.UserProfile.Single(m => m.UserID == id);

                    data.FirstName = model.FirstName;
                    data.LastName = model.LastName;
                    data.EmailID = model.Email;
                    details.PhoneNumberCountryCode = model.CountryCode;
                    details.PhoneNumber = model.Phone;

                    data.ModifiedBy = CurrentUser;
                    data.ModifiedDate = DateTime.Now;
                    details.ModifiedDate = DateTime.Now;

                    context.SaveChanges();

                    return RedirectToAction("ManageAdministrator");
                }
                //Add New Admin
                else
                {
                    var create = context.Users;
                    create.Add(new Users
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        EmailID = model.Email,
                        RoleID = 2,
                        Password = "",
                        CreatedBy = CurrentUser,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = CurrentUser,
                        ModifiedDate = DateTime.Now
                    });

                    context.SaveChanges();

                    var newAdmin = context.Users.Single(m => m.EmailID == model.Email);

                    var details = context.UserProfile;
                    details.Add(new UserProfile
                    {
                        PhoneNumberCountryCode = model.CountryCode,
                        PhoneNumber = model.Phone,
                        AddressLine1 = "",
                        City = "",
                        State = "",
                        ZipCode = "",
                        Country = ""
                    });

                    context.SaveChanges();

                    return RedirectToAction("ManageAdministrator");
                }
            }
        }

        #endregion Add Administrator

        #region Delete Administrator

        public void DeleteAdministrator(int id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var CurrentAdmin = context.Users.Single(m => m.EmailID == User.Identity.Name).UserID;

                var Admin = context.Users.Single(m => m.UserID == id);
                Admin.IsActive = false;
                Admin.ModifiedBy = CurrentAdmin;
                Admin.ModifiedDate = DateTime.Now;

                context.SaveChanges();
            }
        }

        #endregion Delete Administrator

        #region System Configurations

        public ActionResult ManageSystemConfiguration()
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                ViewBag.Show = false;

                var systemConfigurations = context.SystemConfigurations.ToList();

                if (systemConfigurations.Count != 0)
                {
                    SystemConfigurationsModel model = new SystemConfigurationsModel
                    {
                        SupportEmail = systemConfigurations.Single(m => m.Key == "SupportEmailAddress").Value,
                        SupportContact = systemConfigurations.Single(m => m.Key == "SupportContact").Value,
                        DefaultNoteImg = systemConfigurations.Single(m => m.Key == "DefaultBookImage").Value,
                        //DefaultProfileImg = systemConfigurations.Single(m => m.Key == "DefaultProfileImage").Value,
                        Emails = systemConfigurations.Single(m => m.Key == "EmailAddresses").Value,
                        FacebookUrl = systemConfigurations.Single(m => m.Key == "Facebook").Value,
                        TwitterUrl = systemConfigurations.Single(m => m.Key == "Twitter").Value,
                        LinkedinUrl = systemConfigurations.Single(m => m.Key == "Linkedin").Value
                    };

                    model.TempPath = model.DefaultNoteImg;

                    if (model.DefaultNoteImg == null)
                    {
                        model.DefaultNoteImg = "~/Content/images/upload-file.png";
                        ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                        ViewBag.ProfilePicturePreview = "#";
                        ViewBag.HideClass = "";
                        ViewBag.NonHideClass = "hidden";
                        ViewBag.ProfilePictureName = "";
                    }
                    else
                    {
                        ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                        ViewBag.ProfilePicturePreview = model.DefaultNoteImg;
                        ViewBag.ProfilePictureName = Path.GetFileName(model.DefaultNoteImg);
                        ViewBag.HideClass = "hidden";
                        ViewBag.NonHideClass = "";
                    }

                    return View(model);
                }
                else
                {
                    return View();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageSystemConfiguration(SystemConfigurationsModel model)
        {
            ViewBag.Show = false;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new NotesMarketPlaceEntities())
            {
                var systemConfigurations = context.SystemConfigurations.ToList();

                try
                {
                    if (systemConfigurations.Single(m => m.Key == "SupportEmailAddress").Value != model.SupportEmail)
                    {
                        systemConfigurations.Single(m => m.Key == "SupportEmailAddress").Value = model.SupportEmail;
                        systemConfigurations.Single(m => m.Key == "SupportEmailAddress").ModifiedDate = DateTime.Now;
                    }

                    if (systemConfigurations.Single(m => m.Key == "SupportContact").Value != model.SupportContact)
                    {
                        systemConfigurations.Single(m => m.Key == "SupportContact").Value = model.SupportContact;
                        systemConfigurations.Single(m => m.Key == "SupportContact").ModifiedDate = DateTime.Now;
                    }

                    if (systemConfigurations.Single(m => m.Key == "DefaultBookImage").Value != model.DefaultNoteImg)
                    {
                        if (model.DefaultNotePicturePath != null)
                        {
                            string FileNameDelete = System.IO.Path.GetFileName(model.TempPath);
                            string PathPreview = Request.MapPath("~/Content/NotesImages/Images/" + FileNameDelete);
                            FileInfo file = new FileInfo(PathPreview);
                            if (file.Exists)
                            {
                                file.Delete();
                            }
                            //UserProfilePicturePath
                            string userProfilePicturePathFileName = Path.GetFileNameWithoutExtension(model.DefaultNotePicturePath.FileName);
                            string userProfilePicturePathExtension = Path.GetExtension(model.DefaultNotePicturePath.FileName);
                            userProfilePicturePathFileName = userProfilePicturePathFileName + DateTime.Now.ToString("yymmssff") + userProfilePicturePathExtension;

                            ViewBag.ProfilePicture = "~/Content/images/upload-file.png";
                            ViewBag.ProfilePicturePreview = "~/Content/NotesImages/Images/" + userProfilePicturePathFileName;
                            ViewBag.ProfilePictureName = userProfilePicturePathFileName;

                            systemConfigurations.Single(m => m.Key == "DefaultBookImage").Value = "~/Content/NotesImages/Images/" + userProfilePicturePathFileName;
                            systemConfigurations.Single(m => m.Key == "DefaultBookImage").ModifiedDate = DateTime.Now;
                            userProfilePicturePathFileName = Path.Combine(Server.MapPath("~/Content/NotesImages/Images/"), userProfilePicturePathFileName);
                            model.DefaultNotePicturePath.SaveAs(userProfilePicturePathFileName);
                        }
                    }

                    //if (systemConfigurations.Single(m => m.Key == "DefaultProfileImage").Value != model.DefaultProfileImg)
                    //{
                    //    systemConfigurations.Single(m => m.Key == "DefaultProfileImage").Value = model.DefaultProfileImg;
                    //    systemConfigurations.Single(m => m.Key == "DefaultProfileImage").ModifiedDate = DateTime.Now;
                    //}

                    if (systemConfigurations.Single(m => m.Key == "EmailAddresses").Value != model.Emails)
                    {
                        systemConfigurations.Single(m => m.Key == "EmailAddresses").Value = model.Emails;
                        systemConfigurations.Single(m => m.Key == "EmailAddresses").ModifiedDate = DateTime.Now;
                    }

                    if (systemConfigurations.Single(m => m.Key == "Facebook").Value != model.FacebookUrl)
                    {
                        systemConfigurations.Single(m => m.Key == "Facebook").Value = model.FacebookUrl;
                        systemConfigurations.Single(m => m.Key == "Facebook").ModifiedDate = DateTime.Now;
                    }

                    if (systemConfigurations.Single(m => m.Key == "Twitter").Value != model.TwitterUrl)
                    {
                        systemConfigurations.Single(m => m.Key == "Twitter").Value = model.TwitterUrl;
                        systemConfigurations.Single(m => m.Key == "Twitter").ModifiedDate = DateTime.Now;
                    }

                    if (systemConfigurations.Single(m => m.Key == "Linkedin").Value != model.LinkedinUrl)
                    {
                        systemConfigurations.Single(m => m.Key == "Linkedin").Value = model.LinkedinUrl;
                        systemConfigurations.Single(m => m.Key == "Linkedin").ModifiedDate = DateTime.Now;
                    }

                    context.SaveChanges();

                    ViewBag.Show = true;
                    ViewBag.AlertClass = "alert-success";
                    ViewBag.message = "Manage Syatem Configuration Has Been Successfully Updated.";

                    ViewBag.HideClass = "hidden";
                    ViewBag.NonHideClass = "";
                }
                catch (Exception ex)
                {
                    ViewBag.Show = true;
                    ViewBag.AlertClass = "alert-danger";
                    ViewBag.message = ex.Message;
                }

                return View(model);
            }
        }

        #endregion System Configurations

        #region Apply Sorting

        public List<ManageAdministratorModel> ApplySorting(string SortOrder, string SortBy, List<ManageAdministratorModel> result)
        {
            switch (SortBy)
            {
                case "FirstName":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.FirstName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.FirstName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.FirstName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "LastName":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.LastName).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.LastName).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.LastName).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Email":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Email).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Email).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Email).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "DateAdded":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.DateAdded).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "PhoneNo":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Phone).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Phone).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                case "Active":
                    {
                        switch (SortOrder)
                        {
                            case "Asc":
                                {
                                    result = result.OrderBy(x => x.Active).ToList();
                                    break;
                                }
                            case "Desc":
                                {
                                    result = result.OrderByDescending(x => x.Active).ToList();
                                    break;
                                }
                            default:
                                {
                                    result = result.OrderBy(x => x.Active).ToList();
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    result = result.OrderByDescending(x => x.DateAdded).ToList();
                    break;
            }
            return result;
        }

        #endregion Apply Sorting

        #region Apply Pagination

        public List<ManageAdministratorModel> ApplyPagination(List<ManageAdministratorModel> result, int PageNumber)
        {
            ViewBag.TotalPages = Math.Ceiling(result.Count() / 5.0);
            ViewBag.PageNumber = PageNumber;

            result = result.Skip((PageNumber - 1) * 5).Take(5).ToList();

            return result;
        }

        #endregion Apply Pagination

        #region LogOut

        public ActionResult LogOut()
        {
            //if (Session["emailID"] != null)
            //{
            //    Session.Abandon();
            //    return RedirectToAction("Login", "Account");
            //}
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        #endregion LogOut
    }
}