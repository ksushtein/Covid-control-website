using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VKR.Models;
using System.Data;
using System.Threading;

namespace VKR.Controllers
{
    public class HomeController : Controller
    {
        // создаем контекст данных
        ScheduleDBEntities dbContext = new ScheduleDBEntities();

        [HttpGet]
        public ActionResult Index()
        {
            // получаем из бд все объекты Book
            var faculties = dbContext.Faculties;
            // передаем все объекты в динамическое свойство Books в ViewBag
            ViewBag.Faculties = faculties;
            // возвращаем представление
            return View();
        }

        public ActionResult ErrorPage(string analysisResult)
        {
            ViewBag.Message = analysisResult;
            return View();
        }
       
    }
}