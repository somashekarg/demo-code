﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OneDirect.Helper;
using OneDirect.Repository.Interface;
using OneDirect.Models;
using Microsoft.Extensions.Logging;
using OneDirect.Repository;
using Microsoft.AspNetCore.Mvc.Rendering;
using OneDirect.ViewModels;
using OneDirect.Extensions;
using OneDirect.Vsee;
using OneDirect.VSee;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace OneDirect.Controllers
{
    [TypeFilter(typeof(LoginAuthorizeAttribute))]
    public class SupportController : Controller
    {
        private readonly IEquipmentExerciseInterface lIEquipmentExerciseRepository;
        private readonly IUserActivityLogInterface lIUserActivityLogRepository;
        private readonly IUserInterface lIUserRepository;
        private readonly IProtocolInterface lIProtocolInterface;
        private readonly IPatientRxInterface lIPatientRxRepository;
        private readonly ILogger logger;
        private OneDirectContext context;

        public SupportController(OneDirectContext context, ILogger<SupportController> plogger, IPatientRxInterface lIPatientRx, IEquipmentExerciseInterface IEquipmentExerciseRepository)
        {
            logger = plogger;
            lIEquipmentExerciseRepository = IEquipmentExerciseRepository;
            this.context = context;
            lIUserRepository = new UserRepository(context);
            lIProtocolInterface = new ProtocolRepository(context);
            lIPatientRxRepository = lIPatientRx;
            lIUserActivityLogRepository = new UserActivityLogRepository(context);
        }


        //get the list of support staff
        // GET: /<controller>/
        public IActionResult Index()
        {
            var response = new Dictionary<string, object>();
            List<SupportView> pUser = new List<SupportView>();

            try
            {
                logger.LogDebug("Supporter Start");

                pUser = lIUserRepository.getUserListByTypeSup(ConstantsVar.Support);

                ViewBag.userlist = pUser;
                return View();
            }
            catch (Exception ex)
            {
                logger.LogDebug("Support Error: " + ex);
                return null;
            }

        }

        //load the dashboard of support staff
        public IActionResult Dashboard()
        {
            ViewBag.shoulderflexionString = "Forward Flexion";
            ViewBag.shoulderflexionString1 = "External Rotation";
            ViewBag.ankleflexionString = "Flexion";
            ViewBag.ankleextensionString = "Extension";
            ViewBag.kneeflexionString = "Flexion";
            ViewBag.kneeextensionString = "Extension";
            List<EquipmentExercise> lExerciseString = lIEquipmentExerciseRepository.GetEquipmentExerciseString();
            if (lExerciseString != null && lExerciseString.Count > 0)
            {
                var lexString = lExerciseString.Where(x => x.Limb == "Shoulder" && x.ExerciseEnum == "Forward Flexion").FirstOrDefault();
                if (lexString != null)
                {
                    ViewBag.shoulderflexionString = lexString.FlexionString;
                }
                var lexString1 = lExerciseString.Where(x => x.Limb == "Shoulder" && x.ExerciseEnum == "External Rotation").FirstOrDefault();
                if (lexString1 != null)
                {
                    ViewBag.shoulderflexionString1 = lexString1.FlexionString;
                }

                var lexString2 = lExerciseString.Where(x => x.Limb == "Ankle").FirstOrDefault();
                if (lexString2 != null)
                {
                    ViewBag.ankleflexionString = lexString2.FlexionString;
                    ViewBag.ankleextensionString = lexString2.ExtensionString;
                }
                var lexString3 = lExerciseString.Where(x => x.Limb == "Knee").FirstOrDefault();
                if (lexString3 != null)
                {
                    ViewBag.kneeflexionString = lexString3.FlexionString;
                    ViewBag.kneeextensionString = lexString3.ExtensionString;
                }
            }
            List<DashboardView> lDashboardView = null;
            string _uType = HttpContext.Session.GetString("UserType");
            if (HttpContext.Session.GetString("UserId") != null && HttpContext.Session.GetString("UserType") != null && HttpContext.Session.GetString("UserType").ToString() == ConstantsVar.Support.ToString())
            {
                try
                {
                    lDashboardView = lIPatientRxRepository.getDashboardForSupport();
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Error: " + ex);
                }
                return View(lDashboardView);
            }
            else
            {
            }
            return View(null);
        }

        public IActionResult Add()
        {
           
            return View();
        }

        //add the new record for support with unique npi and support id
        [HttpPost]
        public IActionResult Add(UserViewModel pUser)
        {
            pUser.Type = ConstantsVar.Support;
            User _user = lIUserRepository.getUser(pUser.UserId);
            User _user2 = lIUserRepository.getUserNpi(pUser.Npi);
            User _userNew = UserExtension.UserViewModelToUser(pUser);
            if ((_user == null) && (_user2 == null))
            {
                _userNew.Phone = RemoveSpecialChars(_userNew.Phone);
                string _User = lIUserRepository.InsertUser(_userNew);
                if (_User == "Username already exists")
                {
                    TempData["Installer"] = JsonConvert.SerializeObject(_userNew);
                    TempData["msg"] = "<script>Helpers.ShowMessage('User with same Support Id is already registered, please use different one', 1);</script>";
                    pUser.UserId = null;
                    return View(pUser);
                }
            }
            else
            {
                if (_user2 != null)
                {
                    if (_user2.Npi == _userNew.Npi)
                    {
                        TempData["Support"] = JsonConvert.SerializeObject(_userNew);
                        TempData["msg"] = "<script>Helpers.ShowMessage('User with same Npi is already registered, please use different one', 1);</script>";
                        pUser.Npi = null;
                        return View(pUser);
                    }
                }
                if (_user.UserId == _userNew.UserId)
                {
                    TempData["Support"] = JsonConvert.SerializeObject(_userNew);
                    TempData["msg"] = "<script>Helpers.ShowMessage('User with same Support Id is already registered, please use different one', 1);</script>";
                    pUser.UserId = null;
                    return View(pUser);
                }
            }
            return RedirectToAction("Index");
        }

        //get and show the profile of support staff using Id
        public IActionResult Profile(string id)
        {
            User pUser = lIUserRepository.getUser(id);
           
            if (pUser != null)
            {
                UserViewModel _user = UserExtension.UserToUserViewModel(pUser);
                ViewBag.Name = _user.Name;
                return View(_user);
            }
            else
            {
                return View(null);
            }
        }

        //updating the changes in profile of support staff
        [HttpPost]
        public IActionResult Profile(UserViewModel pUser)
        {
            pUser.Type = ConstantsVar.Support;
            if (pUser != null && !string.IsNullOrEmpty(pUser.UserId))
            {
                User luser = lIUserRepository.getUser(pUser.UserId);
                if (luser != null && luser.Npi != pUser.Npi)
                {

                    User _user2 = lIUserRepository.getUserNpi(pUser.Npi);
                    if (_user2 != null)
                    {

                        TempData["Installer"] = JsonConvert.SerializeObject(UserExtension.UserViewModelToUser(pUser));
                        TempData["msg"] = "<script>Helpers.ShowMessage('User with same Npi is already registered, please use different one', 1);</script>";
                        pUser.Npi = null;
                        return View(pUser);

                    }
                    else
                    {
                        User _user = UserExtension.UserViewModelToUser(pUser);
                        _user.Phone = RemoveSpecialChars(_user.Phone);
                        string _result = lIUserRepository.UpdateUser(_user);
                    }
                }
                else
                {
                    User _user = UserExtension.UserViewModelToUser(pUser);
                    _user.Phone = RemoveSpecialChars(_user.Phone);
                    string _result = lIUserRepository.UpdateUser(_user);
                }
            }
            if (HttpContext.Session.GetString("UserType") != null && HttpContext.Session.GetString("UserType").ToString() == ConstantsVar.Support.ToString())
            {
                return RedirectToAction("Profile", new { id = pUser.UserId });
            }
            return RedirectToAction("Index");
        }

        public string RemoveSpecialChars(string str)
        {
            string[] chars = new string[] { "-", " ", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
            for (var i = 0; i < chars.Length; i++)
            {
                if (str.Contains(chars[i]))
                {
                    str = str.Replace(chars[i], "");
                }
            }
            return str;
        }

        //delete the record of support staff
        public IActionResult DeleteSupport(string support)
        {
            try
            {
                var _user = (from p in context.User where p.UserId == support select p).FirstOrDefault();

                context.User.Remove(_user);
                
                int res = context.SaveChanges();
                if (res > 0)
                {
                    VSeeHelper lhelper = new VSeeHelper();
                    DeleteUser luser = new DeleteUser();
                    luser.secretkey = ConfigVars.NewInstance.secretkey;
                    luser.username = _user.UserId;
                    var resUser = lhelper.DeleteUser(luser);
                    if (resUser != null && resUser["status"] == "success")
                    {
                        //Insert to User Activity Log
                        UserActivityLog llog = new UserActivityLog();
                        llog.SessionId = HttpContext.Session.GetString("SessionId");
                        llog.ActivityType = "Update";
                        llog.StartTimeStamp = !string.IsNullOrEmpty(HttpContext.Session.GetString("SessionTime")) ? Convert.ToDateTime(HttpContext.Session.GetString("SessionTime")) : DateTime.Now;
                        llog.Duration = Convert.ToInt32((DateTime.Now - Convert.ToDateTime(HttpContext.Session.GetString("SessionTime"))).TotalSeconds);
                        llog.RecordChangeType = "Delete";
                        llog.RecordType = "SupportStaff";
                        llog.Comment = "Record deleted";
                        llog.RecordJson = JsonConvert.SerializeObject(_user);
                        llog.UserId = HttpContext.Session.GetString("UserId");
                        llog.UserName = HttpContext.Session.GetString("UserName");
                        llog.UserType = HttpContext.Session.GetString("UserType");
                        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("ReviewID")))
                        {
                            llog.ReviewId = HttpContext.Session.GetString("ReviewID");
                        }
                        lIUserActivityLogRepository.InsertUserActivityLog(llog);
                    }

                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Support");
            }
            return RedirectToAction("Index", "Support");
        }
    }

}

