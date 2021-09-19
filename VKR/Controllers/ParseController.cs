
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading;
using System.Web;

using System.Web.Mvc;
using VKR.Parsing;
using VKR.Models;
using System.Net;
using System.IO;

namespace VKR.Controllers
{
    public class ParseController: Controller
    {
        public ActionResult Parsing()
        {
            ViewBag.ParseResult = AddToDB();
            return View();
        }
        public string AddToDB()
        {
            var content = ParseModel.GetRequest("https://www.smtu.ru/ru/listschedule/");
            HtmlParser parser = new HtmlParser();
            var htmlDocument = parser.ParseDocument(content);
            Thread.Sleep(1000);
            var groupScheduleList = htmlDocument?.QuerySelectorAll(".main-content .l-grid > .gr > a")?.Select(x => "https://www.smtu.ru" + x?.GetAttribute("href"))?.ToList() ?? new List<string>();
            if (groupScheduleList.Count < 1)
                return "Парсинг данных не осуществлен, так как на сайте с расписанием нет расписаний групп";

            ParseModel parse = new ParseModel();
            parse.Start(groupScheduleList);
            return "Парсинг данных завершен!";
        }
    }
}