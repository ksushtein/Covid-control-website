using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VKR.Models;

namespace VKR.Controllers
{
    public class AnalysisController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            using (ScheduleDBEntities dbContext = new ScheduleDBEntities())
            {
                if (dbContext.Schedules.Count()<1)
                    return RedirectToAction("ErrorPage", "Home", new { analysisResult = "База данных сервера пуста. Попробуйте позднее" });
                if (dbContext.Schedules.Count() < 4000)
                    return RedirectToAction("ErrorPage", "Home", new { analysisResult = "База данных сервера заполнена не полностью. Скорее всего на данный момент происходит процесс парсинга расписания. Этот процесс завершится в течении получаса." });

                int selectedIndex = 1;
                SelectList groups = new SelectList(dbContext.Groups.ToList(), "Group_number", "Group_number", selectedIndex);
                SelectList lecturers = new SelectList(dbContext.Lecturers.Where(x => x.LecturerName.Length > 1).ToList().OrderBy(x => x.LecturerName), "LecturerName", "LecturerName", selectedIndex);
                ViewBag.Groups = groups;
                ViewBag.Lecturers = lecturers;
                return View();
            }
        }

        [HttpPost]
        public ActionResult Analize(string radioTypePatient, string Group, string Lecturer, DateTime DateSymptom, DateTime DateStopInfect, DateTime DateStopAnalize)
        {
            Analyst a = new Analyst(radioTypePatient, Group, Lecturer, DateSymptom, DateStopInfect, DateStopAnalize);
            string analysisResult = a.MakeResearch();
            if (analysisResult != "Анализ успешно завершен")
                return RedirectToAction("ErrorPage", "Home", new { analysisResult = analysisResult } );

            TempData["analyst"] = a;
            return RedirectToAction("Index", "Result");
        }

    }
}
