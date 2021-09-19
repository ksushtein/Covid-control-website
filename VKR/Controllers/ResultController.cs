using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VKR.Models;

namespace VKR.Controllers
{
    public class ResultController : Controller
    {
        Analyst _analyst;

        public ActionResult Index()
        {
            _analyst = TempData["analyst"] as Analyst; // retrieve data from TempData.
            TempData["analyst"] = _analyst; //save for next request

            if (_analyst == null)
                return RedirectToAction("Index", "Analyis");

            List<AuditoriaOnMap> auditoriasOnMap = _analyst.AuditoriasOnMap;
            TempData["analyst"] = _analyst;
            return View(auditoriasOnMap);
        }

        public ActionResult Details(string nameAud)
        {
            _analyst = TempData["analyst"] as Analyst; // retrieve data from TempData.
            TempData["analyst"] = _analyst; //save for next request

            List<AuditoriaOnMap> auditoriasOnMap = _analyst.AuditoriasOnMap;
            AuditoriaOnMap audOnMap = auditoriasOnMap.FirstOrDefault(a => a.Name == nameAud);
            if (audOnMap == null)
                return HttpNotFound();
            return PartialView(audOnMap);
        }

        public ActionResult FirstFloor()
        {
            return PartialView();
        }

        public ActionResult SecondFloor()
        {
            return PartialView();
        }

        public ActionResult ThirdFloor()
        {
            return PartialView();
        }

        public ActionResult FourthFloor()
        {
            return PartialView();
        }

        public ActionResult FifthFloor()
        {
            return PartialView();
        }


    }
}